using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record InstallationDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid SiteId,
    string SiteName,
    Guid? TechnicianUserId,
    string? TechnicianName,
    DateTimeOffset? ScheduledDate,
    DateTimeOffset? CompletedDate,
    string Status,
    string? ChecklistNotes,
    string? PhotoUrls);

public record CreateInstallationRequest(
    Guid CustomerId,
    Guid SiteId,
    Guid? TechnicianUserId,
    DateTimeOffset? ScheduledDate,
    string Status,
    string? ChecklistNotes,
    string? PhotoUrls);

public record UpdateInstallationRequest(
    Guid? TechnicianUserId,
    DateTimeOffset? ScheduledDate,
    DateTimeOffset? CompletedDate,
    string Status,
    string? ChecklistNotes,
    string? PhotoUrls);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InstallationsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public InstallationsController(CrmDbContext db) => _db = db;

    private async Task<bool> OwnsCustomer(Guid customerId, Guid userId, CancellationToken ct) =>
        await _db.Customers.AnyAsync(c => c.Id == customerId && c.OwnerUserId == userId, ct);

    private static bool TryParse(string s, out InstallationStatus st) => Enum.TryParse(s, ignoreCase: true, out st);

    private static InstallationDto Map(InstallationJob j, string custName, string siteName, string? techName) => new(
        j.Id,
        j.CustomerId,
        custName,
        j.SiteId,
        siteName,
        j.TechnicianUserId,
        techName,
        j.ScheduledDate,
        j.CompletedDate,
        j.Status.ToString(),
        j.ChecklistNotes,
        j.PhotoUrls);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InstallationDto>>> List(CancellationToken ct)
    {
        var uid = User.GetUserId();
        var customerIds = await _db.Customers.Where(c => c.OwnerUserId == uid).Select(c => c.Id).ToListAsync(ct);
        var rows = await _db.InstallationJobs.AsNoTracking()
            .Where(j => customerIds.Contains(j.CustomerId))
            .OrderByDescending(j => j.ScheduledDate ?? DateTimeOffset.MinValue)
            .ToListAsync(ct);
        var result = new List<InstallationDto>();
        foreach (var j in rows)
        {
            var cust = await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == j.CustomerId, ct);
            var site = await _db.Sites.AsNoTracking().FirstAsync(s => s.Id == j.SiteId, ct);
            string? techName = null;
            if (j.TechnicianUserId is { } tid)
                techName = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == tid, ct))?.Name;
            result.Add(Map(j, cust.Name, site.Name, techName));
        }
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InstallationDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var j = await _db.InstallationJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (j is null)
            return NotFound();
        if (!await OwnsCustomer(j.CustomerId, uid, ct))
            return NotFound();
        var cust = await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == j.CustomerId, ct);
        var site = await _db.Sites.AsNoTracking().FirstAsync(s => s.Id == j.SiteId, ct);
        string? techName = null;
        if (j.TechnicianUserId is { } tid)
            techName = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == tid, ct))?.Name;
        return Ok(Map(j, cust.Name, site.Name, techName));
    }

    [HttpPost]
    public async Task<ActionResult<InstallationDto>> Create([FromBody] CreateInstallationRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (!await OwnsCustomer(body.CustomerId, uid, ct))
            return BadRequest("Customer not found.");
        if (!await _db.Sites.AnyAsync(s => s.Id == body.SiteId && s.CustomerId == body.CustomerId, ct))
            return BadRequest("Invalid site.");
        if (body.TechnicianUserId is { } tid && !await _db.Users.AnyAsync(u => u.Id == tid, ct))
            return BadRequest("Technician not found.");

        var j = new InstallationJob
        {
            Id = Guid.NewGuid(),
            CustomerId = body.CustomerId,
            SiteId = body.SiteId,
            TechnicianUserId = body.TechnicianUserId,
            ScheduledDate = body.ScheduledDate,
            Status = st,
            ChecklistNotes = Trim(body.ChecklistNotes),
            PhotoUrls = Trim(body.PhotoUrls),
        };
        _db.InstallationJobs.Add(j);
        await _db.SaveChangesAsync(ct);
        var cust = await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == j.CustomerId, ct);
        var site = await _db.Sites.AsNoTracking().FirstAsync(s => s.Id == j.SiteId, ct);
        string? techName = null;
        if (j.TechnicianUserId is { } x)
            techName = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == x, ct))?.Name;
        return Ok(Map(j, cust.Name, site.Name, techName));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InstallationDto>> Update(Guid id, [FromBody] UpdateInstallationRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var j = await _db.InstallationJobs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (j is null)
            return NotFound();
        if (!await OwnsCustomer(j.CustomerId, uid, ct))
            return Forbid();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (body.TechnicianUserId is { } tid && !await _db.Users.AnyAsync(u => u.Id == tid, ct))
            return BadRequest("Technician not found.");

        j.TechnicianUserId = body.TechnicianUserId;
        j.ScheduledDate = body.ScheduledDate;
        j.CompletedDate = body.CompletedDate;
        j.Status = st;
        j.ChecklistNotes = Trim(body.ChecklistNotes);
        j.PhotoUrls = Trim(body.PhotoUrls);
        await _db.SaveChangesAsync(ct);
        var cust = await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == j.CustomerId, ct);
        var site = await _db.Sites.AsNoTracking().FirstAsync(s => s.Id == j.SiteId, ct);
        string? techName = null;
        if (j.TechnicianUserId is { } x)
            techName = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == x, ct))?.Name;
        return Ok(Map(j, cust.Name, site.Name, techName));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var j = await _db.InstallationJobs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (j is null)
            return NotFound();
        if (!await OwnsCustomer(j.CustomerId, uid, ct))
            return Forbid();
        _db.InstallationJobs.Remove(j);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
