using System.Collections;
using System.Linq.Expressions;
using BookStore.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NUnit.Framework;

namespace BookStore.Tests;

[TestFixture]
public sealed class RequestCancellationTests
{
    [Test]
    public async Task Middleware_MakesRequestTokenAvailableAndClearsItAfterSuccess()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationContext = new RequestCancellationContext();
        var httpContext = CreateHttpContext(cancellationSource.Token);

        Task Next(HttpContext context)
        {
            Assert.That(cancellationContext.CancellationToken, Is.EqualTo(cancellationSource.Token));
            return Task.CompletedTask;
        }

        var middleware = new RequestCancellationMiddleware(Next);
        await middleware.InvokeAsync(httpContext, cancellationContext);

        Assert.That(cancellationContext.CancellationToken, Is.EqualTo(CancellationToken.None));
    }

    [Test]
    public void Middleware_ClearsTokenAndPropagatesUnrelatedCancellation()
    {
        var cancellationContext = new RequestCancellationContext();
        var httpContext = CreateHttpContext(CancellationToken.None);

        Task Next(HttpContext context)
        {
            throw new OperationCanceledException("Unrelated cancellation.");
        }

        var middleware = new RequestCancellationMiddleware(Next);

        Assert.ThrowsAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(httpContext, cancellationContext));
        Assert.That(cancellationContext.CancellationToken, Is.EqualTo(CancellationToken.None));
    }

    [Test]
    public async Task Middleware_HandlesAbortedRequestWithoutWritingResponse()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationContext = new RequestCancellationContext();
        var httpContext = CreateHttpContext(cancellationSource.Token);

        Task Next(HttpContext context)
        {
            throw new OperationCanceledException(context.RequestAborted);
        }

        var middleware = new RequestCancellationMiddleware(Next);
        await middleware.InvokeAsync(httpContext, cancellationContext);

        Assert.That(httpContext.Response.StatusCode, Is.EqualTo(499));
        Assert.That(httpContext.Response.ContentLength, Is.Null);
        Assert.That(cancellationContext.CancellationToken, Is.EqualTo(CancellationToken.None));
    }

    [Test]
    public async Task Middleware_KeepsConcurrentRequestTokensIsolated()
    {
        using var firstSource = new CancellationTokenSource();
        using var secondSource = new CancellationTokenSource();
        var firstCancellationContext = new RequestCancellationContext();
        var secondCancellationContext = new RequestCancellationContext();
        var bothRequestsEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRequests = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var enteredCount = 0;

        async Task WaitForOtherRequestAsync(RequestCancellationContext currentContext, CancellationToken expectedToken)
        {
            Assert.That(currentContext.CancellationToken, Is.EqualTo(expectedToken));
            if (Interlocked.Increment(ref enteredCount) == 2)
            {
                bothRequestsEntered.SetResult();
            }
            await releaseRequests.Task;
            Assert.That(currentContext.CancellationToken, Is.EqualTo(expectedToken));
        }

        Task FirstNext(HttpContext context)
        {
            return WaitForOtherRequestAsync(firstCancellationContext, firstSource.Token);
        }

        Task SecondNext(HttpContext context)
        {
            return WaitForOtherRequestAsync(secondCancellationContext, secondSource.Token);
        }

        var firstMiddleware = new RequestCancellationMiddleware(FirstNext);
        var secondMiddleware = new RequestCancellationMiddleware(SecondNext);
        var firstRequest = firstMiddleware.InvokeAsync(
            CreateHttpContext(firstSource.Token),
            firstCancellationContext);
        var secondRequest = secondMiddleware.InvokeAsync(
            CreateHttpContext(secondSource.Token),
            secondCancellationContext);

        await bothRequestsEntered.Task;
        releaseRequests.SetResult();
        await Task.WhenAll(firstRequest, secondRequest);

        Assert.That(firstCancellationContext.CancellationToken, Is.EqualTo(CancellationToken.None));
        Assert.That(secondCancellationContext.CancellationToken, Is.EqualTo(CancellationToken.None));
    }

    [Test]
    public async Task EfQueryExecutor_ForwardsRequestTokenToEveryOperation()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationContext = new RequestCancellationContext();
        var recorder = new CancellationTokenRecorder();
        var query = new TestAsyncEnumerable<int>(new[] { 1, 2, 3 }, recorder);
        var dbContext = new TrackingDbContext();
        var executor = new EfQueryExecutor(cancellationContext);
        var httpContext = CreateHttpContext(cancellationSource.Token);

        async Task ExecuteOperations(HttpContext context)
        {
            await executor.FirstOrDefaultAsync(query, number => number == 1);
            await executor.AnyAsync(query, number => number == 2);
            await executor.CountAsync(query);
            await executor.ToListAsync(query);
            await executor.SaveChangesAsync(dbContext);
        }

        var middleware = new RequestCancellationMiddleware(ExecuteOperations);
        await middleware.InvokeAsync(httpContext, cancellationContext);

        Assert.That(recorder.Tokens, Has.Count.EqualTo(4));
        Assert.That(recorder.Tokens, Has.All.EqualTo(cancellationSource.Token));
        Assert.That(dbContext.LastSaveToken, Is.EqualTo(cancellationSource.Token));
    }

    private static DefaultHttpContext CreateHttpContext(CancellationToken cancellationToken)
    {
        return new DefaultHttpContext
        {
            RequestAborted = cancellationToken
        };
    }

    private sealed class TrackingDbContext : DbContext
    {
        public CancellationToken LastSaveToken { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            LastSaveToken = cancellationToken;
            return Task.FromResult(0);
        }
    }

    private sealed class CancellationTokenRecorder
    {
        public List<CancellationToken> Tokens { get; } = new List<CancellationToken>();

        public void Record(CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
        }
    }

    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider inner;
        private readonly CancellationTokenRecorder recorder;

        public TestAsyncQueryProvider(IQueryProvider inner, CancellationTokenRecorder recorder)
        {
            this.inner = inner;
            this.recorder = recorder;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression, recorder);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression, recorder);
        }

        public object? Execute(Expression expression)
        {
            return inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            recorder.Record(cancellationToken);
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executeMethod = typeof(IQueryProvider)
                .GetMethods()
                .Single(method => method.Name == nameof(IQueryProvider.Execute)
                    && method.IsGenericMethod
                    && method.GetParameters().Length == 1)
                .MakeGenericMethod(resultType);
            var executionResult = executeMethod.Invoke(inner, new object[] { expression });
            var fromResultMethod = typeof(Task)
                .GetMethods()
                .Single(method => method.Name == nameof(Task.FromResult))
                .MakeGenericMethod(resultType);
            return (TResult)fromResultMethod.Invoke(null, new[] { executionResult })!;
        }
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        private readonly CancellationTokenRecorder recorder;

        public TestAsyncEnumerable(IEnumerable<T> enumerable, CancellationTokenRecorder recorder)
            : base(enumerable)
        {
            this.recorder = recorder;
        }

        public TestAsyncEnumerable(Expression expression, CancellationTokenRecorder recorder)
            : base(expression)
        {
            this.recorder = recorder;
        }

        IQueryProvider IQueryable.Provider
        {
            get
            {
                return new TestAsyncQueryProvider<T>(this, recorder);
            }
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            recorder.Record(cancellationToken);
            return new TestAsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());
        }
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            this.inner = inner;
        }

        public T Current
        {
            get
            {
                return inner.Current;
            }
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
