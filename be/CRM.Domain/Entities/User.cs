using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class User : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public int SerialId { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Name { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Sales;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Lead> OwnedLeads { get; set; } = new List<Lead>();
    public ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    public ICollection<Customer> OwnedCustomers { get; set; } = new List<Customer>();
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
    public ICollection<InstallationJob> InstallationsAsTechnician { get; set; } = new List<InstallationJob>();
    public ICollection<AMCVisit> AMCVisitsAsTechnician { get; set; } = new List<AMCVisit>();
    public ICollection<ServiceRequest> AssignedServiceRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<OpsTask> AssignedOpsTasks { get; set; } = new List<OpsTask>();
}
