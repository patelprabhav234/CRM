namespace CRM.Domain.Entities;

/// <summary>Rows partitioned by <see cref="TenantId"/> (shared DB multi-tenancy).</summary>
public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}
