using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class Site : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public int SerialId { get; set; }
    public Guid TenantId { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public SiteType SiteType { get; set; }
    public string? ComplianceStatus { get; set; }

    public ICollection<InstallationJob> Installations { get; set; } = new List<InstallationJob>();
    public ICollection<AMCContract> AMCContracts { get; set; } = new List<AMCContract>();
}
