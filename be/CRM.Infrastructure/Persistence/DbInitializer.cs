using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await db.Database.MigrateAsync(ct);

        if (await db.Users.AnyAsync(ct))
            return;

        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var techId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var admin = new User
        {
            Id = adminId,
            Email = "admin@fireops.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Name = "FireOps Admin",
            Role = UserRole.Admin,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var tech = new User
        {
            Id = techId,
            Email = "tech@fireops.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
            Name = "Field Technician",
            Role = UserRole.Technician,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.AddRange(admin, tech);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            OwnerUserId = adminId,
            Name = "Shah Fire Safety",
            ContactPerson = "Mr. Shah",
            Phone = "+91 265 1234567",
            Email = "contact@shahfiresafety.example",
            Address = "Vadodara, Gujarat",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
        };
        db.Customers.Add(customer);

        var site = new Site
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Name = "Plant A — Makarpura",
            Address = "GIDC Makarpura",
            City = "Vadodara",
            State = "Gujarat",
            SiteType = SiteType.Industrial,
            ComplianceStatus = "Due inspection Q2",
        };
        db.Sites.Add(site);

        db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "CO2 extinguisher 4.5kg", Category = "Extinguisher", Price = 8500m, Description = "ISI marked" },
            new Product { Id = Guid.NewGuid(), Name = "Hydrant valve set", Category = "Hydrant system", Price = 22000m },
            new Product { Id = Guid.NewGuid(), Name = "AMC annual — combined site", Category = "AMC", Price = 45000m });

        var amc = new AMCContract
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SiteId = site.Id,
            StartDate = DateTimeOffset.UtcNow.AddMonths(-3),
            EndDate = DateTimeOffset.UtcNow.AddMonths(9),
            VisitFrequencyPerYear = 4,
            Status = AMCContractStatus.Active,
            ContractValue = 45000m,
        };
        db.AMCContracts.Add(amc);

        var visit = new AMCVisit
        {
            Id = Guid.NewGuid(),
            AMCContractId = amc.Id,
            ScheduledDate = DateTimeOffset.UtcNow.AddDays(7),
            TechnicianUserId = techId,
            Status = AMCVisitStatus.Scheduled,
        };
        db.AMCVisits.Add(visit);

        db.Leads.Add(new Lead
        {
            Id = Guid.NewGuid(),
            OwnerUserId = adminId,
            Name = "New enquiry — warehouse",
            Company = "Western Logistics",
            Phone = "+91 98765 00000",
            City = "Vadodara",
            State = "Gujarat",
            Requirement = "Hydrant system audit + refill",
            Source = "WhatsApp",
            Status = LeadStatus.Contacted,
            AssignedToUserId = adminId,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        });

        var sr = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SiteId = site.Id,
            Description = "Pressure gauge fault on line 2 hydrant",
            Status = ServiceRequestStatus.Open,
            Priority = "High",
            AssignedToUserId = techId,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        db.ServiceRequests.Add(sr);

        db.InstallationJobs.Add(new InstallationJob
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SiteId = site.Id,
            TechnicianUserId = techId,
            ScheduledDate = DateTimeOffset.UtcNow.AddDays(3),
            Status = InstallationStatus.Scheduled,
            ChecklistNotes = "Mounting brackets, piping as per drawing SF-12",
        });

        db.OpsTasks.Add(new OpsTask
        {
            Id = Guid.NewGuid(),
            Title = "Complete AMC quarterly visit",
            AssignedToUserId = techId,
            DueDate = DateTimeOffset.UtcNow.AddDays(7),
            Status = OpsTaskStatus.Pending,
            TaskType = OpsTaskType.AMC,
            AMCVisitId = visit.Id,
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Database seeded. Admin: admin@fireops.local / Admin123! | Tech: tech@fireops.local / Tech123!");
    }
}
