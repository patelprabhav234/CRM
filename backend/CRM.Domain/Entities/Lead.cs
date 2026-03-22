using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class Lead : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public int SerialId { get; set; }
    public Guid TenantId { get; set; }

    public Guid OwnerUserId { get; set; }
    public User Owner { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    /// <summary>Fire safety requirement: extinguishers, hydrant, audit, training, etc.</summary>
    public string? Requirement { get; set; }

    public string Source { get; set; } = "Call";
    public LeadStatus Status { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedTo { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
