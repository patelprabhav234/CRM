using System.Security.Claims;
using CRM.Domain.Enums;

namespace CRM.Api.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return v is null ? throw new InvalidOperationException("Missing user id claim.") : Guid.Parse(v);
    }

    /// <summary>Admin and Manager can see all tenant records; Sales/Technician are scoped to owner/assignment.</summary>
    public static bool IsTenantAdminOrManager(this ClaimsPrincipal user)
    {
        var r = user.FindFirstValue(ClaimTypes.Role);
        return string.Equals(r, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase);
    }
}
