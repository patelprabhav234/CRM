🧠 MULTI-TENANCY STRATEGY (IMPORTANT)
There are 3 approaches:

Approach	Pros	Cons
Shared DB + TenantId	Simple, scalable	Needs strict filtering
Separate DB per tenant	Strong isolation	Complex ops
Hybrid	Flexible	Complex design
✅ Recommended for YOU:
👉 Shared Database + TenantId (Phase 1)
👉 Upgrade to Hybrid later if needed

🏗️ MULTI-TENANT ARCHITECTURE
🔷 High-Level
[ React App ]
     ↓
[ .NET API ]
     ↓
[Tenant Resolver Middleware]
     ↓
[ Application Layer ]
     ↓
[ Shared PostgreSQL DB (TenantId आधारित)]
🔑 TENANT IDENTIFICATION
Choose one:

Option A — Subdomain (Best SaaS UX)
shah.fireopscrm.com
abc.fireopscrm.com
Option B — Header-based
X-Tenant-ID: shah
Option C — Login-based (Simplest to start)
User logs in → system detects tenant

✅ Recommendation:
Start with:
👉 Login-based → then upgrade to subdomain

🗄️ DATABASE DESIGN (MULTI-TENANT)
🔥 Golden Rule:
Every table must have:

TenantId (UUID / GUID)
Updated Example Tables
Users
Id (PK)
TenantId
Name
Email
PasswordHash
Role
Customers
Id (PK)
TenantId
Name
Phone
Email
Leads
Id (PK)
TenantId
Name
Status
AssignedTo
AMCContracts
Id (PK)
TenantId
CustomerId
StartDate
EndDate
🔐 Add Index
CREATE INDEX idx_tenant ON Customers(TenantId);
👉 Critical for performance

⚙️ BACKEND IMPLEMENTATION (.NET)
1. Tenant Context Service
public interface ITenantService
{
    Guid TenantId { get; }
}
2. Middleware
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public async Task Invoke(HttpContext context, ITenantService tenantService)
    {
        var tenantId = context.User?.Claims
            .FirstOrDefault(c => c.Type == "tenant_id")?.Value;

        tenantService.SetTenant(tenantId);

        await _next(context);
    }
}
3. Global Query Filter (EF Core 🔥)
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Customer>()
        .HasQueryFilter(x => x.TenantId == _tenantService.TenantId);
}
👉 This ensures:

No data leakage

Automatic filtering

4. Base Entity
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
🔐 AUTHENTICATION DESIGN
Use JWT with Tenant:

{
  "userId": "123",
  "tenantId": "abc-123",
  "role": "Admin"
}
🖥️ FRONTEND (REACT)
Tenant Handling
Store Tenant Info
localStorage.setItem("tenantId", tenantId);
Axios Interceptor
axios.interceptors.request.use((config) => {
  config.headers["X-Tenant-ID"] = localStorage.getItem("tenantId");
  return config;
});
🧱 TENANT ONBOARDING FLOW
Step 1: Signup
Company Name

Admin User

Step 2: Create Tenant
Tenants Table
-------------
Id
Name
Subdomain
CreatedAt
Step 3: Seed Data
Default roles

Sample products

📦 TENANTS TABLE (IMPORTANT)
Id (PK)
Name
Subdomain
IsActive
CreatedAt
🎯 DATA ISOLATION RULES
Every query filtered by TenantId

No cross-tenant joins

Admin cannot access other tenant data