using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

public class Quotation
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public User Owner { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    public decimal TotalAmount { get; set; }
    public QuotationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
}
