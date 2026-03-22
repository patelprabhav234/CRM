using CRM.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM.Infrastructure.Persistence;

public class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    public CrmDbContext CreateDbContext(string[] args)
    {
        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost\\SQLEXPRESS;Database=fireops_crm_dev;TrustServerCertificate=True;Trusted_Connection=True;Encrypt=False";

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseSqlServer(cs, sql =>
                sql.MigrationsAssembly(typeof(CrmDbContext).Assembly.GetName().Name))
            .Options;

        return new CrmDbContext(options, new DesignTimeTenantContext());
    }
}
