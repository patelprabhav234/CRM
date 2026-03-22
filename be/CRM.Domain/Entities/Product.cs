namespace CRM.Domain.Entities;

public class Product : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public int SerialId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
