using System.Security.Cryptography.X509Certificates;
using BookStore.IdentityServer;
using BookStore.Infrastructure;
using BookStore.Infrastructure.Identity;
using BookStore.Models;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var commandMode = args.FirstOrDefault() is "--migrate" or "users";
var builder = WebApplication.CreateBuilder(commandMode ? [] : args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
var development = builder.Environment.IsDevelopment();
var oauth = builder.Configuration.GetSection("OAuth").Get<OAuthSettings>() ?? new();
oauth.Validate(development);
var connection = DatabaseConnection.Create(
    builder.Configuration.GetConnectionString("Identity"),
    builder.Configuration["MSSQL_SA_PASSWORD"],
    "Identity");
builder.Services.AddRequestCancellation();
builder.Services.AddIdentityDatabase(connection);
builder.Services.AddIdentity<IdentityUser, IdentityRole>(IdentityOptionsConfiguration.Configure)
    .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(ConfigureApplicationCookie);
var keyPath = builder.Configuration["IdentityServer:KeyDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, ".local", "identity");
Directory.CreateDirectory(keyPath);
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("BookStore.IdentityServer")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(keyPath, "data-protection")));
var identityServer = builder.Services.AddIdentityServer(ConfigureIdentityServer)
    .AddAspNetIdentity<IdentityUser>()
    .AddConfigurationStore<IdentityConfigurationDbContext>(ConfigureConfigurationStore)
    .AddOperationalStore<IdentityOperationalDbContext>(ConfigureOperationalStore);
// Duende configures Identity cookies for cross-site iframes. This app uses top-level redirects,
// so Lax also lets browsers accept the local Development cookie over HTTP.
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    IdentityConstants.ApplicationScheme,
    ConfigureIdentityCookie);
var certificatePath = builder.Configuration["IdentityServer:SigningCertificatePath"];
if (!string.IsNullOrWhiteSpace(certificatePath))
{
    var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath,
        builder.Configuration["IdentityServer:SigningCertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
    if (!certificate.HasPrivateKey || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow
        || certificate.NotBefore.ToUniversalTime() > DateTime.UtcNow)
    {
        throw new InvalidOperationException("The signing certificate must be valid and include its private key.");
    }
    identityServer.AddSigningCredential(certificate);
    dataProtection.ProtectKeysWithCertificate(certificate);
}
else if (development)
{
    identityServer.AddDeveloperSigningCredential(persistKey: true, filename: Path.Combine(keyPath, "signing-key.jwk"));
}
else
{
    throw new InvalidOperationException("Set IdentityServer:SigningCertificatePath outside Development.");
}
builder.Services.AddScoped<IdentityConfigurationSeeder>();
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck<ApplicationIdentityDbContext>>("users", tags: ["ready"])
    .AddCheck<DatabaseHealthCheck<IdentityConfigurationDbContext>>("configuration", tags: ["ready"])
    .AddCheck<DatabaseHealthCheck<IdentityOperationalDbContext>>("grants", tags: ["ready"]);

var app = builder.Build();
if (commandMode)
{
    await using var scope = app.Services.CreateAsyncScope();
    if (args[0] == "--migrate")
    {
        await DatabaseRegistration.MigrateIdentityAsync(scope.ServiceProvider);
        await scope.ServiceProvider.GetRequiredService<IdentityConfigurationSeeder>().SeedAsync(oauth);
    }
    else
    {
        Environment.ExitCode = await UserCommands.RunAsync(args.Skip(1).ToArray(), scope.ServiceProvider);
    }
    return;
}
app.UseExceptionHandler("/Account/Error");
app.UseRequestCancellation();
if (!development)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseMiddleware<SecurityHeadersMiddleware>(oauth.SwaggerRedirectUri);
app.UseStaticFiles();
app.UseRouting();
if (development)
{
    app.UseMiddleware<DevelopmentImplicitSignInMiddleware>(oauth.SearchClientId);
}
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = IsLiveCheck });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = IsReadyCheck });
app.Run();

void ConfigureApplicationCookie(CookieAuthenticationOptions options)
{
    options.Cookie.Name = "bookstore.identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
}

void ConfigureIdentityServer(Duende.IdentityServer.Configuration.IdentityServerOptions options)
{
    options.IssuerUri = oauth.Issuer;
    options.KeyManagement.Enabled = false;
    options.LicenseKey = builder.Configuration["IdentityServer:LicenseKey"];
    options.UserInteraction.LoginUrl = "/Account/Login";
    options.UserInteraction.LogoutUrl = "/Account/Logout";
    options.UserInteraction.ErrorUrl = "/Account/Error";
    options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
    options.Authentication.CheckSessionCookieSameSiteMode = SameSiteMode.Lax;
}

void ConfigureConfigurationStore(Duende.IdentityServer.EntityFramework.Options.ConfigurationStoreOptions options)
{
    options.ConfigureDbContext = ConfigureConfigurationDbContext;
}

void ConfigureConfigurationDbContext(DbContextOptionsBuilder options)
{
    options.UseSqlServer(connection, ConfigureConfigurationSql);
}

static void ConfigureConfigurationSql(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder options)
{
    DatabaseRegistration.ConfigureSql(options, "__ConfigurationMigrations");
}

void ConfigureOperationalStore(Duende.IdentityServer.EntityFramework.Options.OperationalStoreOptions options)
{
    options.ConfigureDbContext = ConfigureOperationalDbContext;
    options.EnableTokenCleanup = !commandMode;
}

void ConfigureOperationalDbContext(DbContextOptionsBuilder options)
{
    options.UseSqlServer(connection, ConfigureOperationalSql);
}

static void ConfigureOperationalSql(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder options)
{
    DatabaseRegistration.ConfigureSql(options, "__OperationalMigrations");
}

void ConfigureIdentityCookie(CookieAuthenticationOptions options)
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
}

static bool IsLiveCheck(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration check)
{
    return false;
}

static bool IsReadyCheck(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration check)
{
    return check.Tags.Contains("ready");
}
