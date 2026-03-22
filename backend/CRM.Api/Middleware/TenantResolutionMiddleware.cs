using CRM.Infrastructure.Tenancy;

namespace CRM.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext http, ITenantContext tenant)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            var tid = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tid, out var g))
                tenant.SetTenant(g);
        }

        await _next(http);
    }
}
