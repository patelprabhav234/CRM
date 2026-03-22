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
            ?? "Server=xhirejob.postgres.database.azure.com;Database=fireops_crm_dev;Port=5432;User Id=xhirejob;Password=Maan@2026;Ssl Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=300;Keepalive=30;";

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(cs, npgsql =>
                npgsql.MigrationsAssembly("CRM.Infrastructure"))
            .Options;

        return new CrmDbContext(options, new DesignTimeTenantContext());
    }
}
