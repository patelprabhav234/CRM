using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class AMCVisit
{
    public Guid Id { get; set; }
    public Guid AMCContractId { get; set; }
    public AMCContract Contract { get; set; } = null!;

    public DateTimeOffset ScheduledDate { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }

    public Guid? TechnicianUserId { get; set; }
    public User? Technician { get; set; }

    public AMCVisitStatus Status { get; set; }

    public ICollection<OpsTask> LinkedTasks { get; set; } = new List<OpsTask>();
}
