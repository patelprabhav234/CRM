using System.Security.Claims;

namespace CRM.Api.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return v is null ? throw new InvalidOperationException("Missing user id claim.") : Guid.Parse(v);
    }
}
