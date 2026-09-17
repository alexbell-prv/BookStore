using BookStore.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API;

public sealed class BookStoreExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetails;

    public BookStoreExceptionHandler(IProblemDetailsService problemDetails)
    {
        this.problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BookStoreException failure)
        {
            return false;
        }

        int status;
        switch (failure.Kind)
        {
            case FailureKind.Validation:
                status = StatusCodes.Status400BadRequest;
                break;
            case FailureKind.NotFound:
                status = StatusCodes.Status404NotFound;
                break;
            case FailureKind.Conflict:
                status = StatusCodes.Status409Conflict;
                break;
            default:
                status = StatusCodes.Status500InternalServerError;
                break;
        }

        context.Response.StatusCode = status;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = failure.Kind.ToString(),
                Detail = failure.Message
            }
        });
        return true;
    }
}
