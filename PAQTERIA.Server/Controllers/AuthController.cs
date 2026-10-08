using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Contracts;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;
using PAQTERIA.Server.Security;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IPasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUser>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await database.Users.Include(candidate => candidate.Role)
            .SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        var role = AppRoles.Canonicalize(user?.Role.Name);
        if (user is null || role is not (AppRoles.Administrator or AppRoles.WarehouseManager))
            return InvalidCredentials();

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return InvalidCredentials();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await database.SaveChangesAsync(cancellationToken);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

        return Ok(new AuthenticatedUser(user.Id, user.Name, user.Email, role));
    }

    [Authorize(Policy = "OperationalStaff")]
    [HttpGet("me")]
    public ActionResult<AuthenticatedUser> Me()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Unauthorized();
        var role = User.FindFirstValue(ClaimTypes.Role);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        return role is null || name is null || email is null
            ? Unauthorized()
            : Ok(new AuthenticatedUser(id, name, email, role));
    }

    [Authorize(Policy = "OperationalStaff")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private UnauthorizedObjectResult InvalidCredentials() => Unauthorized(new ProblemDetails
    {
        Title = "No se pudo iniciar sesión",
        Detail = "Revisa el correo y la contraseña.",
        Status = StatusCodes.Status401Unauthorized
    });
}
