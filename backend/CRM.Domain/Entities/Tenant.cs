namespace CRM.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public int SerialId { get; set; }
    public string Name { get; set; } = null!;
    /// <summary>Lowercase slug used at login (Phase 1); subdomain routing later.</summary>
    public string Subdomain { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
