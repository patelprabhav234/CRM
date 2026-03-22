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

        // Round-trip 1: leads count + customer ids in parallel (were sequential before).
        var leadsTask = tenantWide
            ? _db.Leads.CountAsync(ct)
            : _db.Leads.CountAsync(l => l.OwnerUserId == uid, ct);
        var customersTask = tenantWide
            ? _db.Customers.Select(c => c.Id).ToListAsync(ct)
            : _db.Customers.Where(c => c.OwnerUserId == uid).Select(c => c.Id).ToListAsync(ct);
        await Task.WhenAll(leadsTask, customersTask);
        var totalLeads = await leadsTask;
        var customerIds = await customersTask;

        // Round-trip 2: all aggregates that depend on customerIds (and pending quotes) in parallel.
        var activeAmcTask = _db.AMCContracts
            .CountAsync(c => customerIds.Contains(c.CustomerId) && c.Status == AMCContractStatus.Active && c.EndDate >= now, ct);
        var openSrTask = _db.ServiceRequests.CountAsync(
            s => customerIds.Contains(s.CustomerId) &&
                 (s.Status == ServiceRequestStatus.Open || s.Status == ServiceRequestStatus.InProgress), ct);
        var pendingQuotesTask = tenantWide
            ? _db.Quotations.CountAsync(
                q => q.Status == QuotationStatus.Draft || q.Status == QuotationStatus.Sent, ct)
            : _db.Quotations.CountAsync(
                q => q.OwnerUserId == uid && (q.Status == QuotationStatus.Draft || q.Status == QuotationStatus.Sent), ct);
        var amcRevenueTask = _db.AMCContracts
            .Where(c => customerIds.Contains(c.CustomerId) && c.Status == AMCContractStatus.Active && c.EndDate >= now && c.ContractValue != null)
            .SumAsync(c => c.ContractValue ?? 0, ct);
        var myContractIdsTask = _db.AMCContracts
            .Where(c => customerIds.Contains(c.CustomerId))
            .Select(c => c.Id)
            .ToListAsync(ct);
        await Task.WhenAll(activeAmcTask, openSrTask, pendingQuotesTask, amcRevenueTask, myContractIdsTask);

        var activeAmc = await activeAmcTask;
        var openSr = await openSrTask;
        var pendingQuotes = await pendingQuotesTask;
        var amcRevenue = await amcRevenueTask;
        var myContractIds = await myContractIdsTask;

        // Round-trip 3: visits only when there are contracts to check.
        var upcomingVisits = 0;
        if (myContractIds.Count > 0)
        {
            upcomingVisits = await _db.AMCVisits.CountAsync(
                v => myContractIds.Contains(v.AMCContractId) && v.Status == AMCVisitStatus.Scheduled && v.ScheduledDate >= now && v.ScheduledDate <= in30,
                ct);
        }

        return Ok(new FireOpsDashboardDto(
            totalLeads,
            activeAmc,
            openSr,
            pendingQuotes,
            amcRevenue,
            upcomingVisits));
    }
}
