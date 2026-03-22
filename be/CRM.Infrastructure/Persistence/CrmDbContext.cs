using CRM.Domain.Entities;
using CRM.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence;

public class CrmDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    /// <summary>Per-request tenant for global query filters (DbContext member so EF evaluates per query).</summary>
    public Guid CurrentTenantId => _tenantContext?.TenantId ?? Guid.Empty;

    public CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<InstallationJob> InstallationJobs => Set<InstallationJob>();
    public DbSet<AMCContract> AMCContracts => Set<AMCContract>();
    public DbSet<AMCVisit> AMCVisits => Set<AMCVisit>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<OpsTask> OpsTasks => Set<OpsTask>();

    public override int SaveChanges()
    {
        StampTenantOnScopedEntities();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenantOnScopedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTenantOnScopedEntities()
    {
        var tid = _tenantContext.TenantId;
        if (tid == Guid.Empty)
            return;

        foreach (var entry in ChangeTracker.Entries<ITenantScopedEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                entry.Entity.TenantId = tid;
        }
    }

    /*
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants");
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.Subdomain).HasMaxLength(100);
            e.HasIndex(x => x.Subdomain).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Tenant).WithMany(x => x.Users).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Lead>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Company).HasMaxLength(200);
            e.Property(x => x.Source).HasMaxLength(100);
            e.Property(x => x.Requirement).HasMaxLength(2000);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Owner)
                .WithMany(x => x.OwnedLeads)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            e.HasOne(x => x.AssignedTo)
                .WithMany(x => x.AssignedLeads)
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.ContactPerson).HasMaxLength(200);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Owner).WithMany(x => x.OwnedCustomers).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Site>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.ComplianceStatus).HasMaxLength(200);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Customer).WithMany(x => x.Sites).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<Quotation>(e =>
        {
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Owner).WithMany(x => x.Quotations).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(x => x.Quotations).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuotationItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Quotation).WithMany(x => x.Items).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstallationJob>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Site).WithMany(x => x.Installations).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Technician).WithMany(x => x.InstallationsAsTechnician).HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AMCContract>(e =>
        {
            e.Property(x => x.ContractValue).HasPrecision(18, 2);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Site).WithMany(x => x.AMCContracts).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AMCVisit>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Contract).WithMany(x => x.Visits).HasForeignKey(x => x.AMCContractId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Technician).WithMany(x => x.AMCVisitsAsTechnician).HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ServiceRequest>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Customer).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AssignedTo).WithMany(x => x.AssignedServiceRequests).HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OpsTask>(e =>
        {
            e.ToTable("OpsTasks");
            e.Property(x => x.Title).HasMaxLength(500);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.AssignedTo).WithMany(x => x.AssignedOpsTasks).HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ServiceRequest).WithMany(x => x.LinkedTasks).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AMCVisit).WithMany(x => x.LinkedTasks).HasForeignKey(x => x.AMCVisitId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.InstallationJob).WithMany(x => x.LinkedTasks).HasForeignKey(x => x.InstallationJobId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == CurrentTenantId);
        modelBuilder.Entity<Lead>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<Customer>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<Site>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<Quotation>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<QuotationItem>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<InstallationJob>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<AMCContract>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<AMCVisit>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<ServiceRequest>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<OpsTask>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
    }
    */
}
