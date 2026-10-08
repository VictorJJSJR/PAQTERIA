using System.Globalization;
using System.Security;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;
using PAQTERIA.Server.Security;

namespace PAQTERIA.Server.Services;

public static class UserProvisioning
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("El alta inicial necesita una consola interactiva para solicitar la contraseña de forma segura.");

        Console.WriteLine("Alta segura de una cuenta de PAQTERIA (la contraseña no se guarda en el historial del shell).");
        Console.Write("Nombre completo: ");
        var name = (Console.ReadLine() ?? string.Empty).Trim();
        Console.Write("Correo: ");
        var email = (Console.ReadLine() ?? string.Empty).Trim();
        Console.Write("Rol (Administrator / Warehouse Manager): ");
        var roleName = AppRoles.Canonicalize(Console.ReadLine());
        if (name.Length is < 2 or > 150 || !IsValidEmail(email) || roleName is not (AppRoles.Administrator or AppRoles.WarehouseManager))
            throw new InvalidOperationException("Nombre, correo o rol inválido.");

        var password = ReadSecret("Contraseña (mínimo 12 caracteres): ");
        var confirmation = ReadSecret("Confirma la contraseña: ");
        if (password.Length < 12 || !string.Equals(password, confirmation, StringComparison.Ordinal))
            throw new InvalidOperationException("La contraseña debe tener al menos 12 caracteres y coincidir con su confirmación.");

        await using var scope = services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;
        var factory = scopedServices.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var hasher = scopedServices.GetRequiredService<IPasswordHasher<AppUser>>();
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        if (await database.Users.AnyAsync(cancellationToken))
        {
            Console.WriteLine("Hay usuarios existentes. Para crear otra cuenta de personal, usa la pantalla «Usuarios» con una sesión de Administrador.");
            return;
        }
        if (roleName != AppRoles.Administrator)
            throw new InvalidOperationException("La primera cuenta debe tener el rol Administrator.");

        var role = await database.Roles.SingleOrDefaultAsync(item => item.Name == roleName, cancellationToken)
            ?? throw new InvalidOperationException("No existe el rol Administrator. Ejecuta primero database/paqteria-app-setup.sql.");
        if (await database.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new InvalidOperationException("Ya existe una cuenta con ese correo.");

        var user = new AppUser
        {
            RoleId = role.Id,
            Role = role,
            Name = name,
            Email = email,
            RegisteredAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        database.Users.Add(user);
        await database.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"Cuenta Administrator creada para {email}. Ya puedes iniciar sesión en PAQTERIA.");
        password = string.Empty;
        confirmation = string.Empty;
    }

    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);
        var secret = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return secret.ToString();
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0) secret.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar)) secret.Append(key.KeyChar);
        }
    }

    private static bool IsValidEmail(string email) => email.Length <= 150
        && System.Text.RegularExpressions.Regex.IsMatch(email,
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}
