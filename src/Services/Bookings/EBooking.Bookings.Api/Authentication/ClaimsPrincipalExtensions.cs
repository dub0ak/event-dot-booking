namespace EBooking.Bookings.Api;

using System.Security.Claims;

/// <summary>
/// Методы чтения пользовательских данных из JWT claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(
        this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "JWT token does not contain a valid user identifier.");
        }

        return userId;
    }

    public static bool IsAdmin(
        this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.IsInRole("Admin");
    }
}