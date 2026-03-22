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

        var tenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == tenantId, ct))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Shah Fire Safety (Demo)",
                Subdomain = "demo",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }

        // Add the primary tenant from backend_setup.md
        var primaryTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == primaryTenantId, ct))
        {
            db.Tenants.Add(new Tenant
            {
                Id = primaryTenantId,
                Name = "Shah Fire & Safety (Mechanical Div)",
                Subdomain = "shah-fire",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }

        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var techId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var primaryAdminId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == adminId, ct))
        {
            db.Users.Add(new User
            {
                Id = adminId,
                TenantId = tenantId,
                Email = "admin@fireops.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Name = "FireOps Admin",
                Role = UserRole.Admin,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == techId, ct))
        {
            db.Users.Add(new User
            {
                Id = techId,
                TenantId = tenantId,
                Email = "tech@fireops.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                Name = "Field Technician",
                Role = UserRole.Technician,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == primaryAdminId, ct))
        {
            db.Users.Add(new User
            {
                Id = primaryAdminId,
                TenantId = primaryTenantId,
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Name = "Shah Admin",
                Role = UserRole.Admin,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);

        // Seed products for shah-fire
        db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), TenantId = primaryTenantId, Name = "High Pressure Steam Piping", Category = "Utility Piping", Price = 85000, Description = "Steam distribution for industrial heating" },
            new Product { Id = Guid.NewGuid(), TenantId = primaryTenantId, Name = "RO Water Distribution Loop", Category = "Water Systems", Price = 45000, Description = "Piping for reverse osmosis filtered water" },
            new Product { Id = Guid.NewGuid(), TenantId = primaryTenantId, Name = "Utility Pump House Installation", Category = "Pump House", Price = 250000, Description = "Complete utility pump station assembly" }
        );

        // Seed leads for shah-fire
        db.Leads.AddRange(
            new Lead { Id = Guid.NewGuid(), TenantId = primaryTenantId, OwnerUserId = primaryAdminId, Name = "Mr. Rajesh", Company = "Gujarat Pharma Ltd", Email = "rajesh@guja-pharma.com", Phone = "9900011122", Source = "Website", Status = LeadStatus.New, CreatedAt = DateTimeOffset.UtcNow },
            new Lead { Id = Guid.NewGuid(), TenantId = primaryTenantId, OwnerUserId = primaryAdminId, Name = "Sunita Gupta", Company = "Evergreen Resorts", Email = "sunita@evergreen.com", Phone = "9900022233", Source = "Referral", Status = LeadStatus.Contacted, CreatedAt = DateTimeOffset.UtcNow }
        );

        // Ensure sujan@gmail.com and their tenant exist
        var sujan = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "sujan@gmail.com", ct);
        if (sujan == null)
        {
            var sujanTenantId = Guid.NewGuid();
            db.Tenants.Add(new Tenant
            {
                Id = sujanTenantId,
                Name = "Sujan Enterprises",
                Subdomain = "sujan",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            sujan = new User
            {
                Id = Guid.NewGuid(),
                TenantId = sujanTenantId,
                Email = "sujan@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sujan123!"),
                Name = "Sujan Kumar",
                Role = UserRole.Admin,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Users.Add(sujan);
            await db.SaveChangesAsync(ct);
        }

        var sTid = sujan.TenantId;
        var sUid = sujan.Id;

        // Seed Products for Sujan
        if (!await db.Products.IgnoreQueryFilters().AnyAsync(p => p.TenantId == sTid, ct))
        {
            db.Products.AddRange(
                new Product { Id = Guid.NewGuid(), TenantId = sTid, Name = "Industrial Fire Extinguisher (ABC)", Category = "Safety", Price = 4500, IsActive = true, Description = "High capacity multipurpose extinguisher" },
                new Product { Id = Guid.NewGuid(), TenantId = sTid, Name = "Smoke Detector - Wireless", Category = "Electronics", Price = 1200, IsActive = true, Description = "IoT enabled smoke sensor" },
                new Product { Id = Guid.NewGuid(), TenantId = sTid, Name = "Fire Hydrant Pump 5HP", Category = "Mechanical", Price = 85000, IsActive = true, Description = "Main pump for hydrant systems" }
            );
            await db.SaveChangesAsync(ct);
        }

        // Seed Leads for Sujan
        if (!await db.Leads.IgnoreQueryFilters().AnyAsync(l => l.TenantId == sTid, ct))
        {
            db.Leads.AddRange(
                new Lead { Id = Guid.NewGuid(), TenantId = sTid, OwnerUserId = sUid, Name = "Inquiry: Warehouse Safety Audit", Company = "Logix Warehouse", Email = "manager@logix.com", Status = LeadStatus.New, Source = "Website", CreatedAt = DateTimeOffset.UtcNow },
                new Lead { Id = Guid.NewGuid(), TenantId = sTid, OwnerUserId = sUid, Name = "Refill Service Request", Company = "Imperial Mall", Email = "facility@imperial.com", Status = LeadStatus.Contacted, Source = "Call", CreatedAt = DateTimeOffset.UtcNow }
            );
        }

        // Seed Customer, Site, AMC, etc.
        if (!await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.TenantId == sTid, ct))
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                OwnerUserId = sUid,
                Name = "Grand Plaza Hotel",
                ContactPerson = "Mr. Verma",
                Email = "verma@grandplaza.com",
                Phone = "9876501234",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Customers.Add(customer);

            var site = new Site
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                CustomerId = customer.Id,
                Name = "Main Hotel Complex",
                Address = "Colaba Road",
                City = "Mumbai",
                State = "Maharashtra",
                SiteType = SiteType.Commercial,
                ComplianceStatus = "Compliant"
            };
            db.Sites.Add(site);

            var amc = new AMCContract
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                CustomerId = customer.Id,
                SiteId = site.Id,
                StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(11),
                VisitFrequencyPerYear = 4,
                Status = AMCContractStatus.Active,
                ContractValue = 35000
            };
            db.AMCContracts.Add(amc);

            var visit = new AMCVisit
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                AMCContractId = amc.Id,
                ScheduledDate = DateTimeOffset.UtcNow.AddDays(15),
                Status = AMCVisitStatus.Scheduled,
                TechnicianUserId = sUid,
            };
            db.AMCVisits.Add(visit);

            db.ServiceRequests.Add(new ServiceRequest
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                CustomerId = customer.Id,
                SiteId = site.Id,
                Description = "Kitchen fire alarm keeps beeping",
                Status = ServiceRequestStatus.Open,
                Priority = "High",
                CreatedAt = DateTimeOffset.UtcNow
            });

            db.InstallationJobs.Add(new InstallationJob
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                CustomerId = customer.Id,
                SiteId = site.Id,
                ScheduledDate = DateTimeOffset.UtcNow.AddDays(5),
                Status = InstallationStatus.Scheduled,
                TechnicianUserId = sUid
            });

            db.OpsTasks.Add(new OpsTask
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                Title = "Conduct initial safety audit",
                AssignedToUserId = sUid,
                DueDate = DateTimeOffset.UtcNow.AddDays(2),
                Status = OpsTaskStatus.Pending,
                TaskType = OpsTaskType.Other
            });

            // Add one Quotation
            var firstProd = await db.Products.IgnoreQueryFilters().FirstAsync(p => p.TenantId == sTid, ct);
            db.Quotations.Add(new Quotation
            {
                Id = Guid.NewGuid(),
                TenantId = sTid,
                CustomerId = customer.Id,
                SiteId = site.Id,
                OwnerUserId = sUid,
                TotalAmount = firstProd.Price * 2,
                Status = QuotationStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow,
                Items = new List<QuotationItem>
                {
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = sTid, ProductId = firstProd.Id, Quantity = 2, UnitPrice = firstProd.Price }
                }
            });
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Successfully dynamically seeded data for sujan@gmail.com (Tenant: {TenantId})", sTid);
        // --- END: Dynamic Seeding ---

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
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
            TenantId = tenantId,
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
            new Product
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "CO2 extinguisher 4.5kg",
                Category = "Extinguisher",
                Price = 8500m,
                Description = "ISI marked",
            },
            new Product
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Hydrant valve set",
                Category = "Hydrant system",
                Price = 22000m,
            },
            new Product
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "AMC annual — combined site",
                Category = "AMC",
                Price = 45000m,
            });

        var amc = new AMCContract
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
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
            TenantId = tenantId,
            AMCContractId = amc.Id,
            ScheduledDate = DateTimeOffset.UtcNow.AddDays(7),
            TechnicianUserId = techId,
            Status = AMCVisitStatus.Scheduled,
        };
        db.AMCVisits.Add(visit);

        db.Leads.Add(new Lead
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
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
            TenantId = tenantId,
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
            TenantId = tenantId,
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
            TenantId = tenantId,
            Title = "Complete AMC quarterly visit",
            AssignedToUserId = techId,
            DueDate = DateTimeOffset.UtcNow.AddDays(7),
            Status = OpsTaskStatus.Pending,
            TaskType = OpsTaskType.AMC,
            AMCVisitId = visit.Id,
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Database seeded. Tenant subdomain: demo | Admin: admin@fireops.local / Admin123! | Tech: tech@fireops.local / Tech123!");
    }
}
