using CRM.Infrastructure.Tenancy;

namespace CRM.Infrastructure.Persistence;

/// <summary>Used only by EF design-time tools; query filters use <see cref="Guid.Empty"/>.</summary>
public sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Empty;
    public void SetTenant(Guid tenantId) { }
}
