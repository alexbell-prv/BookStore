using BookStore.API;
using BookStore.API.Controllers;
using BookStore.BusinessLogic;
using BookStore.Host;
using BookStore.Infrastructure;
using BookStore.Infrastructure.Identity;
using BookStore.Models;
using BookStore.Repository;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var migrate = args.Contains("--migrate", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--migrate").ToArray());
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
var oauth = builder.Configuration.GetSection("OAuth").Get<OAuthSettings>() ?? new();
oauth.Validate(builder.Environment.IsDevelopment());
var connection = DatabaseConnection.Create(
    builder.Configuration.GetConnectionString("BookStore"),
    builder.Configuration["MSSQL_SA_PASSWORD"],
    "BookStore");
var identityConnection = DatabaseConnection.Create(
    builder.Configuration.GetConnectionString("Identity"),
    builder.Configuration["MSSQL_SA_PASSWORD"],
    "Identity");
builder.Services.AddRequestCancellation();
builder.Services.AddBookStoreDatabase(connection);
builder.Services.AddIdentityDatabase(identityConnection);
builder.Services.AddIdentityCore<IdentityUser>(IdentityOptionsConfiguration.Configure)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddControllers().AddApplicationPart(typeof(BooksController).Assembly);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BookStoreExceptionHandler>();
builder.Services.AddBookStoreAuthentication(oauth, builder.Environment.IsDevelopment());
builder.Services.AddBookStoreOpenApi(oauth);
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck<BookStoreDbContext>>("database", tags: ["ready"]);

var app = builder.Build();
if (migrate)
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BookStoreDbContext>().Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseRequestCancellation();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = IsLiveCheck });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = IsReadyCheck });
if (builder.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.MapOpenApi();
    app.UseSwaggerUI(ConfigureSwagger);
    app.MapGet("/", RedirectToSwagger).ExcludeFromDescription();
}
app.Run();

void ConfigureSwagger(Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIOptions options)
{
    options.SwaggerEndpoint("/openapi/v1.json", "Bookstore API v1");
    options.OAuthAppName("Bookstore Swagger");
    options.OAuthClientId(oauth.SearchClientId);
    options.OAuthScopes(Security.SearchBooks);
    options.OAuth2RedirectUrl(oauth.SwaggerRedirectUri);
    options.ConfigObject.PersistAuthorization = false;
}

static IResult RedirectToSwagger()
{
    return Results.Redirect("/swagger");
}

static bool IsLiveCheck(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration check)
{
    return false;
}

static bool IsReadyCheck(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration check)
{
    return check.Tags.Contains("ready");
}
