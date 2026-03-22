namespace CRM.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public User Owner { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Site> Sites { get; set; } = new List<Site>();
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
