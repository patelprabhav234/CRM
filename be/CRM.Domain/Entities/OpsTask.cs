using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

/// <summary>Technician / ops tasks (AMC, service, installation).</summary>
public class OpsTask : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Title { get; set; } = null!;

    public Guid AssignedToUserId { get; set; }
    public User AssignedTo { get; set; } = null!;

    public DateTimeOffset DueDate { get; set; }
    public OpsTaskStatus Status { get; set; }
    public OpsTaskType TaskType { get; set; }

    public Guid? ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }

    public Guid? AMCVisitId { get; set; }
    public AMCVisit? AMCVisit { get; set; }

    public Guid? InstallationJobId { get; set; }
    public InstallationJob? InstallationJob { get; set; }
}
