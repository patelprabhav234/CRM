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
        try
        {
            await db.Database.MigrateAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database migration failed. This often happens in development if tables already exist. Skipping migration and attempting to seed data.");
        }

        // ── Look up the live "Shah Fire Safety" tenant by subdomain ──
        var shahTenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Subdomain == "shah-fire", ct);

        if (shahTenant == null)
        {
            logger.LogWarning("Tenant 'shah-fire' not found in DB. Skipping seed.");
            return;
        }

        var tid = shahTenant.Id;

        // ── Ensure at least one admin user for this tenant ──
        var adminUser = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tid && u.Role == UserRole.Admin, ct);

        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tid,
                Email = "admin@shahfire.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Name = "Shah Admin",
                Role = UserRole.Admin,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync(ct);
        }

        // Ensure a technician user
        var techUser = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tid && u.Role == UserRole.Technician, ct);

        if (techUser == null)
        {
            techUser = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tid,
                Email = "tech@shahfire.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                Name = "Ramesh Patel",
                Role = UserRole.Technician,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Users.Add(techUser);
            await db.SaveChangesAsync(ct);
        }

        var adminId = adminUser.Id;
        var techId = techUser.Id;

        // ── Guard: only seed once ──
        if (await db.Products.IgnoreQueryFilters().AnyAsync(p => p.TenantId == tid, ct))
        {
            logger.LogInformation("Data already seeded for tenant '{Name}'. Skipping.", shahTenant.Name);
            return;
        }

        // ═══════════════════════════════════════════════════════
        //  1. PRODUCTS  (fire-safety domain)
        // ═══════════════════════════════════════════════════════
        var prod1 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "ABC Fire Extinguisher 6kg",       Category = "Extinguisher",  Price = 3500m,  IsActive = true, Description = "Multipurpose dry chemical powder extinguisher, ISI marked" };
        var prod2 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "CO₂ Fire Extinguisher 4.5kg",     Category = "Extinguisher",  Price = 8500m,  IsActive = true, Description = "Carbon dioxide extinguisher for electrical fires" };
        var prod3 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "Fire Hydrant Valve Set",          Category = "Hydrant System",Price = 22000m, IsActive = true, Description = "Landing valve with coupling and accessories" };
        var prod4 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "Fire Alarm Control Panel 8-Zone", Category = "Fire Alarm",    Price = 45000m, IsActive = true, Description = "Addressable fire alarm panel with LCD display" };
        var prod5 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "Sprinkler Head (Pendent 68°C)",   Category = "Sprinkler",     Price = 750m,   IsActive = true, Description = "Glass bulb type UL-listed sprinkler" };
        var prod6 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "Smoke Detector – Photoelectric",  Category = "Fire Alarm",    Price = 1800m,  IsActive = true, Description = "Ceiling-mount photoelectric smoke sensor" };
        var prod7 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "Fire Hydrant Pump 10HP",          Category = "Pump House",    Price = 125000m,IsActive = true, Description = "Centrifugal pump for hydrant boosting" };
        var prod8 = new Product { Id = Guid.NewGuid(), TenantId = tid, Name = "Hose Reel Drum (30m)",            Category = "Hydrant System",Price = 18000m, IsActive = true, Description = "Swinging type hose reel with nozzle" };

        db.Products.AddRange(prod1, prod2, prod3, prod4, prod5, prod6, prod7, prod8);

        // ═══════════════════════════════════════════════════════
        //  2. LEADS
        // ═══════════════════════════════════════════════════════
        db.Leads.AddRange(
            new Lead { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Rajesh Mehta",   Company = "Gujarat Pharma Ltd",        Email = "rajesh@gujaratpharma.com",   Phone = "9876501111", City = "Ahmedabad",  State = "Gujarat", Source = "Website",  Status = LeadStatus.New,       Requirement = "Fire hydrant audit for new pharma plant",          CreatedAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new Lead { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Sunita Gupta",   Company = "Evergreen Resorts",         Email = "sunita@evergreen.com",      Phone = "9876502222", City = "Vadodara",   State = "Gujarat", Source = "Referral", Status = LeadStatus.Contacted, Requirement = "Complete fire safety setup for 5-star resort",     CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),  AssignedToUserId = adminId },
            new Lead { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Vikram Singh",   Company = "Western Logistics",         Email = "vikram@westernlog.com",     Phone = "9876503333", City = "Surat",      State = "Gujarat", Source = "WhatsApp", Status = LeadStatus.SiteVisit, Requirement = "Warehouse extinguisher refill + hydrant audit",    CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),  AssignedToUserId = techId },
            new Lead { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Priya Patel",    Company = "Stellar Textiles",          Email = "priya@stellartex.com",      Phone = "9876504444", City = "Rajkot",     State = "Gujarat", Source = "Call",     Status = LeadStatus.Quoted,    Requirement = "Factory fire alarm system + sprinkler installation",CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),  AssignedToUserId = adminId },
            new Lead { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Arjun Desai",    Company = "Navratna Infrastructure",   Email = "arjun@navratna.com",        Phone = "9876505555", City = "Vadodara",   State = "Gujarat", Source = "Trade Show", Status = LeadStatus.Won,   Requirement = "Full fire protection for high-rise project",       CreatedAt = DateTimeOffset.UtcNow.AddDays(-15), AssignedToUserId = adminId }
        );

        // ═══════════════════════════════════════════════════════
        //  3. CUSTOMERS  (3 customers with sites)
        // ═══════════════════════════════════════════════════════
        var cust1 = new Customer { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Gujarat Pharma Ltd",       ContactPerson = "Mr. Rajesh Mehta",   Phone = "9876501111", Email = "rajesh@gujaratpharma.com",  Address = "GIDC Ankleshwar, Gujarat",       CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6) };
        var cust2 = new Customer { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Imperial Mall & Hotels",   ContactPerson = "Mr. Verma",          Phone = "9876506666", Email = "verma@imperialmall.com",    Address = "Alkapuri, Vadodara",             CreatedAt = DateTimeOffset.UtcNow.AddMonths(-4) };
        var cust3 = new Customer { Id = Guid.NewGuid(), TenantId = tid, OwnerUserId = adminId, Name = "Navratna Infrastructure",  ContactPerson = "Mr. Arjun Desai",    Phone = "9876505555", Email = "arjun@navratna.com",        Address = "Gotri Road, Vadodara",           CreatedAt = DateTimeOffset.UtcNow.AddMonths(-2) };
        db.Customers.AddRange(cust1, cust2, cust3);

        // Sites
        var site1 = new Site { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust1.Id, Name = "Pharma Plant – Ankleshwar",     Address = "GIDC Phase 2, Ankleshwar",  City = "Ankleshwar", State = "Gujarat", SiteType = SiteType.Industrial,  ComplianceStatus = "Due Inspection Q2" };
        var site2 = new Site { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust1.Id, Name = "Pharma Warehouse – Vadodara",   Address = "Makarpura GIDC",            City = "Vadodara",   State = "Gujarat", SiteType = SiteType.Industrial,  ComplianceStatus = "Compliant" };
        var site3 = new Site { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust2.Id, Name = "Imperial Mall – Main Building",  Address = "Alkapuri Circle",           City = "Vadodara",   State = "Gujarat", SiteType = SiteType.Commercial,  ComplianceStatus = "Compliant" };
        var site4 = new Site { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust2.Id, Name = "Imperial Hotel – Tower Block",   Address = "Race Course Road",          City = "Vadodara",   State = "Gujarat", SiteType = SiteType.Commercial,  ComplianceStatus = "Renewal Pending" };
        var site5 = new Site { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust3.Id, Name = "Navratna Heights – Phase 1",     Address = "Gotri Road",                City = "Vadodara",   State = "Gujarat", SiteType = SiteType.Commercial,  ComplianceStatus = "Under Review" };
        db.Sites.AddRange(site1, site2, site3, site4, site5);

        // ═══════════════════════════════════════════════════════
        //  4. QUOTATIONS  (3 quotations with line items)
        // ═══════════════════════════════════════════════════════
        var q1Id = Guid.NewGuid();
        var q2Id = Guid.NewGuid();
        var q3Id = Guid.NewGuid();

        db.Quotations.AddRange(
            new Quotation
            {
                Id = q1Id, TenantId = tid, CustomerId = cust1.Id, SiteId = site1.Id, OwnerUserId = adminId,
                TotalAmount = 22000m + (750m * 20),
                Status = QuotationStatus.Sent, CreatedAt = DateTimeOffset.UtcNow.AddDays(-7),
                Items = new List<QuotationItem>
                {
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = tid, ProductId = prod3.Id, Quantity = 1, UnitPrice = 22000m },
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = tid, ProductId = prod5.Id, Quantity = 20, UnitPrice = 750m },
                }
            },
            new Quotation
            {
                Id = q2Id, TenantId = tid, CustomerId = cust2.Id, SiteId = site3.Id, OwnerUserId = adminId,
                TotalAmount = 45000m + (1800m * 15),
                Status = QuotationStatus.Approved, CreatedAt = DateTimeOffset.UtcNow.AddDays(-12),
                Items = new List<QuotationItem>
                {
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = tid, ProductId = prod4.Id, Quantity = 1, UnitPrice = 45000m },
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = tid, ProductId = prod6.Id, Quantity = 15, UnitPrice = 1800m },
                }
            },
            new Quotation
            {
                Id = q3Id, TenantId = tid, CustomerId = cust3.Id, SiteId = site5.Id, OwnerUserId = adminId,
                TotalAmount = (3500m * 30) + (8500m * 10) + 125000m,
                Status = QuotationStatus.Draft, CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                Items = new List<QuotationItem>
                {
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = tid, ProductId = prod1.Id, Quantity = 30, UnitPrice = 3500m },
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = tid, ProductId = prod2.Id, Quantity = 10, UnitPrice = 8500m },
                    new QuotationItem { Id = Guid.NewGuid(), TenantId = tid, ProductId = prod7.Id, Quantity = 1,  UnitPrice = 125000m },
                }
            }
        );

        // ═══════════════════════════════════════════════════════
        //  5. INSTALLATIONS
        // ═══════════════════════════════════════════════════════
        var inst1 = new InstallationJob { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust1.Id, SiteId = site1.Id, TechnicianUserId = techId, ScheduledDate = DateTimeOffset.UtcNow.AddDays(3),  Status = InstallationStatus.Scheduled,  ChecklistNotes = "Install hydrant valve set + 20 sprinkler heads as per drawing GP-01" };
        var inst2 = new InstallationJob { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust2.Id, SiteId = site3.Id, TechnicianUserId = techId, ScheduledDate = DateTimeOffset.UtcNow.AddDays(-5), Status = InstallationStatus.Completed, CompletedDate = DateTimeOffset.UtcNow.AddDays(-4), ChecklistNotes = "8-zone fire alarm panel + 15 smoke detectors installed and tested" };
        var inst3 = new InstallationJob { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust3.Id, SiteId = site5.Id, TechnicianUserId = techId, ScheduledDate = DateTimeOffset.UtcNow.AddDays(10), Status = InstallationStatus.Scheduled,  ChecklistNotes = "Full fire protection: extinguishers, CO₂ units, 10HP pump" };
        var inst4 = new InstallationJob { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust2.Id, SiteId = site4.Id, TechnicianUserId = techId, ScheduledDate = DateTimeOffset.UtcNow.AddDays(1),  Status = InstallationStatus.InProgress, ChecklistNotes = "Hose reel drum installation on floors 3-8" };
        db.InstallationJobs.AddRange(inst1, inst2, inst3, inst4);

        // ═══════════════════════════════════════════════════════
        //  6. AMC CONTRACTS & VISITS
        // ═══════════════════════════════════════════════════════
        var amc1 = new AMCContract { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust1.Id, SiteId = site1.Id, StartDate = DateTimeOffset.UtcNow.AddMonths(-3), EndDate = DateTimeOffset.UtcNow.AddMonths(9), VisitFrequencyPerYear = 4, Status = AMCContractStatus.Active,  ContractValue = 48000m };
        var amc2 = new AMCContract { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust2.Id, SiteId = site3.Id, StartDate = DateTimeOffset.UtcNow.AddMonths(-6), EndDate = DateTimeOffset.UtcNow.AddMonths(6), VisitFrequencyPerYear = 2, Status = AMCContractStatus.Active,  ContractValue = 35000m };
        var amc3 = new AMCContract { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust1.Id, SiteId = site2.Id, StartDate = DateTimeOffset.UtcNow.AddMonths(-14),EndDate = DateTimeOffset.UtcNow.AddMonths(-2),VisitFrequencyPerYear = 4, Status = AMCContractStatus.Expired, ContractValue = 42000m };
        db.AMCContracts.AddRange(amc1, amc2, amc3);

        var visit1 = new AMCVisit { Id = Guid.NewGuid(), TenantId = tid, AMCContractId = amc1.Id, ScheduledDate = DateTimeOffset.UtcNow.AddDays(-30), CompletedDate = DateTimeOffset.UtcNow.AddDays(-30), TechnicianUserId = techId, Status = AMCVisitStatus.Completed };
        var visit2 = new AMCVisit { Id = Guid.NewGuid(), TenantId = tid, AMCContractId = amc1.Id, ScheduledDate = DateTimeOffset.UtcNow.AddDays(15),  TechnicianUserId = techId, Status = AMCVisitStatus.Scheduled };
        var visit3 = new AMCVisit { Id = Guid.NewGuid(), TenantId = tid, AMCContractId = amc2.Id, ScheduledDate = DateTimeOffset.UtcNow.AddDays(-60), CompletedDate = DateTimeOffset.UtcNow.AddDays(-59), TechnicianUserId = techId, Status = AMCVisitStatus.Completed };
        var visit4 = new AMCVisit { Id = Guid.NewGuid(), TenantId = tid, AMCContractId = amc2.Id, ScheduledDate = DateTimeOffset.UtcNow.AddDays(30),  TechnicianUserId = techId, Status = AMCVisitStatus.Scheduled };
        db.AMCVisits.AddRange(visit1, visit2, visit3, visit4);

        // ═══════════════════════════════════════════════════════
        //  7. SERVICE REQUESTS
        // ═══════════════════════════════════════════════════════
        var sr1 = new ServiceRequest { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust1.Id, SiteId = site1.Id, Description = "Pressure gauge fault on line 2 hydrant",              Status = ServiceRequestStatus.Open,       Priority = "High",   AssignedToUserId = techId,  CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        var sr2 = new ServiceRequest { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust2.Id, SiteId = site3.Id, Description = "False alarm triggered on 3rd floor smoke detector",  Status = ServiceRequestStatus.InProgress, Priority = "Medium", AssignedToUserId = techId,  CreatedAt = DateTimeOffset.UtcNow.AddDays(-3) };
        var sr3 = new ServiceRequest { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust2.Id, SiteId = site4.Id, Description = "Hose reel drum leaking on 5th floor",               Status = ServiceRequestStatus.Open,       Priority = "High",   AssignedToUserId = techId,  CreatedAt = DateTimeOffset.UtcNow.AddHours(-6) };
        var sr4 = new ServiceRequest { Id = Guid.NewGuid(), TenantId = tid, CustomerId = cust1.Id, SiteId = site2.Id, Description = "Annual extinguisher refill and inspection needed",   Status = ServiceRequestStatus.Resolved,   Priority = "Low",    AssignedToUserId = techId,  CreatedAt = DateTimeOffset.UtcNow.AddDays(-20) };
        db.ServiceRequests.AddRange(sr1, sr2, sr3, sr4);

        // ═══════════════════════════════════════════════════════
        //  8. TASKS  (linked to AMC visits, SRs, installations)
        // ═══════════════════════════════════════════════════════
        db.OpsTasks.AddRange(
            // AMC tasks
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Complete quarterly AMC visit – Gujarat Pharma", AssignedToUserId = techId,  DueDate = DateTimeOffset.UtcNow.AddDays(15), Status = OpsTaskStatus.Pending,    TaskType = OpsTaskType.AMC,          AMCVisitId = visit2.Id },
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Complete half-yearly AMC visit – Imperial Mall", AssignedToUserId = techId, DueDate = DateTimeOffset.UtcNow.AddDays(30), Status = OpsTaskStatus.Pending,    TaskType = OpsTaskType.AMC,          AMCVisitId = visit4.Id },
            // Service tasks
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Fix hydrant pressure gauge – Ankleshwar plant", AssignedToUserId = techId,  DueDate = DateTimeOffset.UtcNow.AddDays(2),  Status = OpsTaskStatus.InProgress, TaskType = OpsTaskType.Service,       ServiceRequestId = sr1.Id },
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Investigate false alarm – Imperial Mall 3F",    AssignedToUserId = techId,  DueDate = DateTimeOffset.UtcNow.AddDays(1),  Status = OpsTaskStatus.InProgress, TaskType = OpsTaskType.Service,       ServiceRequestId = sr2.Id },
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Repair hose reel leak – Imperial Hotel 5F",     AssignedToUserId = techId,  DueDate = DateTimeOffset.UtcNow.AddDays(1),  Status = OpsTaskStatus.Pending,    TaskType = OpsTaskType.Service,       ServiceRequestId = sr3.Id },
            // Installation tasks
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Pre-installation site survey – Gujarat Pharma", AssignedToUserId = techId,  DueDate = DateTimeOffset.UtcNow.AddDays(2),  Status = OpsTaskStatus.Pending,    TaskType = OpsTaskType.Installation,  InstallationJobId = inst1.Id },
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Complete hose reel installation – Imperial Hotel", AssignedToUserId = techId, DueDate = DateTimeOffset.UtcNow.AddDays(3), Status = OpsTaskStatus.InProgress, TaskType = OpsTaskType.Installation, InstallationJobId = inst4.Id },
            // General tasks
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Prepare monthly safety compliance report",      AssignedToUserId = adminId, DueDate = DateTimeOffset.UtcNow.AddDays(5),  Status = OpsTaskStatus.Pending,    TaskType = OpsTaskType.General },
            new OpsTask { Id = Guid.NewGuid(), TenantId = tid, Title = "Review and renew expired AMC – Pharma Warehouse",AssignedToUserId = adminId, DueDate = DateTimeOffset.UtcNow.AddDays(7),  Status = OpsTaskStatus.Pending,    TaskType = OpsTaskType.General }
        );

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "✅ Seeded all modules for tenant '{Name}' (Id: {TenantId}): 8 Products, 5 Leads, 3 Customers, 5 Sites, 3 Quotations, 4 Installations, 3 AMC Contracts, 4 AMC Visits, 4 Service Requests, 9 Tasks",
            shahTenant.Name, tid);
    }
}
