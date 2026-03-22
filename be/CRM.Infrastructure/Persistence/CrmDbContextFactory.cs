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
            ?? "Host=localhost;Database=fireops_crm_dev;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(cs, npgsql =>
                npgsql.MigrationsAssembly("CRM.Infrastructure"))
            .Options;

        return new CrmDbContext(options, new DesignTimeTenantContext());
    }
}
