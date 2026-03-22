using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class AMCContract : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid SiteId { get; set; }
    public Site Site { get; set; } = null!;

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int VisitFrequencyPerYear { get; set; }
    public AMCContractStatus Status { get; set; }
    public decimal? ContractValue { get; set; }

    public ICollection<AMCVisit> Visits { get; set; } = new List<AMCVisit>();
}
