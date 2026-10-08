using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Contracts;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;
using PAQTERIA.Server.Security;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdministratorOnly")]
public sealed class UsersController(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IPasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItem>>> GetAll(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await database.Users.AsNoTracking()
            .OrderBy(user => user.Name)
            .Select(user => new { user.Id, user.Name, user.Email, user.Phone, user.RegisteredAt, RoleName = user.Role.Name })
            .ToListAsync(cancellationToken);
        return Ok(rows.Select(user => new UserListItem(user.Id, user.Name, user.Email, user.Phone,
            AppRoles.Canonicalize(user.RoleName) ?? user.RoleName, user.RegisteredAt)).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<UserListItem>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var canonicalRole = AppRoles.Canonicalize(request.Role);
        var creatableRoles = new[] { AppRoles.Administrator, AppRoles.WarehouseManager, AppRoles.Driver };
        if (canonicalRole is null || !creatableRoles.Contains(canonicalRole, StringComparer.Ordinal))
            return BadRequest(new ProblemDetails { Title = "Solo se pueden crear cuentas administrativas, de almacén o de repartidor." });

        var email = request.Email.Trim();
        var name = request.Name.Trim();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await database.Users.AnyAsync(user => user.Email == email, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Ya existe una cuenta con ese correo." });

        var role = await database.Roles.SingleOrDefaultAsync(item => item.Name == canonicalRole, cancellationToken);
        if (role is null)
            return Conflict(new ProblemDetails
            {
                Title = "Falta un rol del sistema",
                Detail = "Vuelve a ejecutar database/paqteria-app-setup.sql para crear los roles necesarios."
            });

        var user = new AppUser
        {
            RoleId = role.Id,
            Role = role,
            Name = name,
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            RegisteredAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        database.Users.Add(user);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails { Title = "Ya existe una cuenta con ese correo." });
        }

        return CreatedAtAction(nameof(GetAll), new UserListItem(user.Id, user.Name, user.Email, user.Phone,
            canonicalRole, user.RegisteredAt));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };
}
