using System.Security.Claims;

namespace CoreFitness.Management.Auth;

public static class AppUsers
{
    public const string AdminRole = "Admin";
    public const string FuelBarRole = "FuelBar";

    private static readonly IReadOnlyList<AppUser> Users =
    [
        new("admin", "admin123", "Gym Owner", AdminRole),
        new("fuelbar", "fuel123", "Fuel Bar", FuelBarRole)
    ];

    public static AppUser? Validate(string username, string password)
    {
        return Users.FirstOrDefault(user =>
            user.Username.Equals(username?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            user.Password == password);
    }

    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, "CoreFitnessCookie");
        return new ClaimsPrincipal(identity);
    }
}

public sealed record AppUser(string Username, string Password, string DisplayName, string Role);
