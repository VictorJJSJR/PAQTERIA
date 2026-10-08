using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Data;

namespace PAQTERIA.Server.Security;

/// <summary>Vuelve a comprobar la cuenta y el rol en SQL Server en cada solicitud autenticada.</summary>
public sealed class PaqteriaCookieEvents(IDbContextFactory<ApplicationDbContext> contextFactory)
    : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idText, out var userId))
        {
            await RejectAsync(context);
            return;
        }

        await using var database = await contextFactory.CreateDbContextAsync(context.HttpContext.RequestAborted);
        var user = await database.Users.AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new { candidate.Id, candidate.Name, candidate.Email, RoleName = candidate.Role.Name })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
        var role = AppRoles.Canonicalize(user?.RoleName);
        if (user is null || role is null || !AppRoles.Staff.Contains(role, StringComparer.Ordinal))
        {
            await RejectAsync(context);
            return;
        }

        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity is null)
        {
            await RejectAsync(context);
            return;
        }

        ReplaceClaim(identity, ClaimTypes.Name, user.Name);
        ReplaceClaim(identity, ClaimTypes.Email, user.Email);
        ReplaceClaim(identity, ClaimTypes.Role, role);
    }

    private static void ReplaceClaim(ClaimsIdentity identity, string type, string value)
    {
        var old = identity.FindFirst(type);
        if (old?.Value == value) return;
        if (old is not null) identity.RemoveClaim(old);
        identity.AddClaim(new Claim(type, value));
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
