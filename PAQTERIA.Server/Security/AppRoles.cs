using System.Globalization;
using System.Text;

namespace PAQTERIA.Server.Security;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string WarehouseManager = "Warehouse Manager";
    public const string Customer = "Customer";
    public const string Driver = "Driver";

    public static readonly string[] All = [Administrator, WarehouseManager, Customer, Driver];
    public static readonly string[] Staff = [Administrator, WarehouseManager];

    public static string? Canonicalize(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return null;
        var key = Normalize(roleName);
        return key switch
        {
            "administrator" or "admin" or "administrador" => Administrator,
            "warehousemanager" or "gerentedealmacen" or "encargadodealmacen" or "jefedealmacen" => WarehouseManager,
            "customer" or "client" or "cliente" => Customer,
            "driver" or "courier" or "repartidor" => Driver,
            _ => null
        };
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var ascii = string.Concat(decomposed.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
        return string.Concat(ascii.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }
}
