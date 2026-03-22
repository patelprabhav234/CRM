using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record SiteDto(Guid Id, Guid CustomerId, string Name, string? Address, string? City, string? State, string SiteType, string? ComplianceStatus);

public record CreateSiteRequest(string Name, string? Address, string? City, string? State, string SiteType, string? ComplianceStatus);
public record UpdateSiteRequest(string Name, string? Address, string? City, string? State, string SiteType, string? ComplianceStatus);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SitesController : ControllerBase
{
    private readonly CrmDbContext _db;

    public SitesController(CrmDbContext db) => _db = db;

    private async Task<bool> OwnsCustomer(Guid customerId, Guid userId, CancellationToken ct) =>
        await _db.Customers.AnyAsync(c => c.Id == customerId && c.OwnerUserId == userId, ct);

    private static bool TrySiteType(string s, out SiteType t) => Enum.TryParse(s, ignoreCase: true, out t);

    private static SiteDto Map(Site s) => new(s.Id, s.CustomerId, s.Name, s.Address, s.City, s.State, s.SiteType.ToString(), s.ComplianceStatus);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> List([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var q = _db.Sites.AsNoTracking().Join(
            _db.Customers.Where(c => c.OwnerUserId == uid),
            s => s.CustomerId,
            c => c.Id,
            (s, _) => s);
        if (customerId is { } cid)
            q = q.Where(s => s.CustomerId == cid);
        var rows = await q.OrderBy(s => s.Name).ToListAsync(ct);
        return Ok(rows.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SiteDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var s = await _db.Sites.AsNoTracking()
            .Join(_db.Customers.Where(c => c.OwnerUserId == uid), x => x.CustomerId, c => c.Id, (site, _) => site)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null)
            return NotFound();
        return Ok(Map(s));
    }

    [HttpPost]
    public async Task<ActionResult<SiteDto>> Create([FromQuery] Guid customerId, [FromBody] CreateSiteRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        if (!await OwnsCustomer(customerId, uid, ct))
            return BadRequest("Customer not found.");

        if (!TrySiteType(body.SiteType, out var st))
            return BadRequest("Invalid site type.");

        var site = new Site
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Name = body.Name.Trim(),
            Address = Trim(body.Address),
            City = Trim(body.City),
            State = Trim(body.State),
            SiteType = st,
            ComplianceStatus = Trim(body.ComplianceStatus),
        };
        _db.Sites.Add(site);
        await _db.SaveChangesAsync(ct);
        return Ok(Map(site));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SiteDto>> Update(Guid id, [FromBody] UpdateSiteRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (site is null)
            return NotFound();
        if (!await OwnsCustomer(site.CustomerId, uid, ct))
            return Forbid();

        if (!TrySiteType(body.SiteType, out var st))
            return BadRequest("Invalid site type.");

        site.Name = body.Name.Trim();
        site.Address = Trim(body.Address);
        site.City = Trim(body.City);
        site.State = Trim(body.State);
        site.SiteType = st;
        site.ComplianceStatus = Trim(body.ComplianceStatus);
        await _db.SaveChangesAsync(ct);
        return Ok(Map(site));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (site is null)
            return NotFound();
        if (!await OwnsCustomer(site.CustomerId, uid, ct))
            return Forbid();
        _db.Sites.Remove(site);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
