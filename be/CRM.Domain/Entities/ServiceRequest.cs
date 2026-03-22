using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class ServiceRequest : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    public string Description { get; set; } = null!;
    public ServiceRequestStatus Status { get; set; }
    public string? Priority { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedTo { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<OpsTask> LinkedTasks { get; set; } = new List<OpsTask>();
}
