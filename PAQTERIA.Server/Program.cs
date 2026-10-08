using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Hubs;
using PAQTERIA.Server.Models;
using PAQTERIA.Server.Security;
using PAQTERIA.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PAQTERIA")
    ?? throw new InvalidOperationException(
        "Falta ConnectionStrings:PAQTERIA. Configura appsettings.Development.json o la variable ConnectionStrings__PAQTERIA.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<TabularFileReader>();
builder.Services.AddHostedService<DatabaseChangeWatcher>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<PaqteriaCookieEvents>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Paqteria.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.EventsType = typeof(PaqteriaCookieEvents);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OperationalStaff", policy =>
        policy.RequireAuthenticatedUser().RequireRole(AppRoles.Staff));
    options.AddPolicy("AdministratorOnly", policy =>
        policy.RequireAuthenticatedUser().RequireRole(AppRoles.Administrator));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

if (args.Any(argument => argument.Equals("--create-user", StringComparison.OrdinalIgnoreCase)))
{
    await UserProvisioning.RunAsync(app.Services);
    return;
}

app.UseDefaultFiles();
app.MapStaticAssets();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHub<OperationsHub>("/hubs/operations");
app.MapFallbackToFile("/index.html");

app.Run();
