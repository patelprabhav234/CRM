using CRM.Api.Extensions;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record FireOpsDashboardDto(
    int TotalLeads,
    int ActiveAmcContracts,
    int OpenServiceRequests,
    int PendingQuotations,
    decimal AmcRevenueActive,
    int UpcomingAmcVisitsNext30Days);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly CrmDbContext _db;

    public DashboardController(CrmDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<ActionResult<FireOpsDashboardDto>> Summary(CancellationToken ct)
    {
        var uid = User.GetUserId();
        var tenantWide = User.IsTenantAdminOrManager();
        var now = DateTimeOffset.UtcNow;
        var in30 = now.AddDays(30);

        var totalLeads = tenantWide
            ? await _db.Leads.CountAsync(ct)
            : await _db.Leads.CountAsync(l => l.OwnerUserId == uid, ct);

        var customerIds = tenantWide
            ? await _db.Customers.Select(c => c.Id).ToListAsync(ct)
            : await _db.Customers.Where(c => c.OwnerUserId == uid).Select(c => c.Id).ToListAsync(ct);

        var activeAmc = await _db.AMCContracts
            .CountAsync(c => customerIds.Contains(c.CustomerId) && c.Status == AMCContractStatus.Active && c.EndDate >= now, ct);

        var openSr = await _db.ServiceRequests.CountAsync(
            s => customerIds.Contains(s.CustomerId) &&
                 (s.Status == ServiceRequestStatus.Open || s.Status == ServiceRequestStatus.InProgress), ct);

        var pendingQuotes = tenantWide
            ? await _db.Quotations.CountAsync(
                q => q.Status == QuotationStatus.Draft || q.Status == QuotationStatus.Sent, ct)
            : await _db.Quotations.CountAsync(
                q => q.OwnerUserId == uid && (q.Status == QuotationStatus.Draft || q.Status == QuotationStatus.Sent), ct);

        var amcRevenue = await _db.AMCContracts
            .Where(c => customerIds.Contains(c.CustomerId) && c.Status == AMCContractStatus.Active && c.EndDate >= now && c.ContractValue != null)
            .SumAsync(c => c.ContractValue ?? 0, ct);

        var myContractIds = await _db.AMCContracts
            .Where(c => customerIds.Contains(c.CustomerId))
            .Select(c => c.Id)
            .ToListAsync(ct);

        var upcomingVisits = await _db.AMCVisits.CountAsync(
            v => myContractIds.Contains(v.AMCContractId) && v.Status == AMCVisitStatus.Scheduled && v.ScheduledDate >= now && v.ScheduledDate <= in30,
            ct);

        return Ok(new FireOpsDashboardDto(
            totalLeads,
            activeAmc,
            openSr,
            pendingQuotes,
            amcRevenue,
            upcomingVisits));
    }
}
