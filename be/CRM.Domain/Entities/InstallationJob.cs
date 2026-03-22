using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class InstallationJob : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid SiteId { get; set; }
    public Site Site { get; set; } = null!;

    public Guid? TechnicianUserId { get; set; }
    public User? Technician { get; set; }

    public DateTimeOffset? ScheduledDate { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public InstallationStatus Status { get; set; }

    public string? ChecklistNotes { get; set; }
    public string? PhotoUrls { get; set; }

    public ICollection<OpsTask> LinkedTasks { get; set; } = new List<OpsTask>();
}
