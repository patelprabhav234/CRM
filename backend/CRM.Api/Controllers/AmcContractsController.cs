using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record AmcContractDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid SiteId,
    string SiteName,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int VisitFrequencyPerYear,
    string Status,
    decimal? ContractValue);

public record CreateAmcContractRequest(
    Guid CustomerId,
    Guid SiteId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int VisitFrequencyPerYear,
    string Status,
    decimal? ContractValue);

public record UpdateAmcContractRequest(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int VisitFrequencyPerYear,
    string Status,
    decimal? ContractValue);

[ApiController]
[Authorize]
[Route("api/amc/contracts")]
public class AmcContractsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public AmcContractsController(CrmDbContext db) => _db = db;

    private async Task<bool> OwnsCustomer(Guid customerId, Guid userId, CancellationToken ct) =>
        User.IsTenantAdminOrManager()
            ? await _db.Customers.AnyAsync(c => c.Id == customerId, ct)
            : await _db.Customers.AnyAsync(c => c.Id == customerId && c.OwnerUserId == userId, ct);

    private static bool TryParse(string s, out AMCContractStatus st) => Enum.TryParse(s, ignoreCase: true, out st);

    private static AmcContractDto Map(AMCContract c, string custName, string siteName) => new(
        c.Id,
        c.CustomerId,
        custName,
        c.SiteId,
        siteName,
        c.StartDate,
        c.EndDate,
        c.VisitFrequencyPerYear,
        c.Status.ToString(),
        c.ContractValue);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AmcContractDto>>> List(CancellationToken ct)
    {
        var uid = User.GetUserId();
        var customerIds = User.IsTenantAdminOrManager()
            ? await _db.Customers.Select(c => c.Id).ToListAsync(ct)
            : await _db.Customers.Where(c => c.OwnerUserId == uid).Select(c => c.Id).ToListAsync(ct);
        var rows = await _db.AMCContracts.AsNoTracking()
            .Where(x => customerIds.Contains(x.CustomerId))
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(ct);
        var list = new List<AmcContractDto>();
        foreach (var c in rows)
        {
            var cust = await _db.Customers.AsNoTracking().FirstAsync(x => x.Id == c.CustomerId, ct);
            var site = await _db.Sites.AsNoTracking().FirstAsync(x => x.Id == c.SiteId, ct);
            list.Add(Map(c, cust.Name, site.Name));
        }
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AmcContractDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var c = await _db.AMCContracts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
            return NotFound();
        if (!await OwnsCustomer(c.CustomerId, uid, ct))
            return NotFound();
        var cust = await _db.Customers.AsNoTracking().FirstAsync(x => x.Id == c.CustomerId, ct);
        var site = await _db.Sites.AsNoTracking().FirstAsync(x => x.Id == c.SiteId, ct);
        return Ok(Map(c, cust.Name, site.Name));
    }

    [HttpPost]
    public async Task<ActionResult<AmcContractDto>> Create([FromBody] CreateAmcContractRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (!await OwnsCustomer(body.CustomerId, uid, ct))
            return BadRequest("Customer not found.");
        if (!await _db.Sites.AnyAsync(s => s.Id == body.SiteId && s.CustomerId == body.CustomerId, ct))
            return BadRequest("Invalid site.");

        var c = new AMCContract
        {
            Id = Guid.NewGuid(),
            CustomerId = body.CustomerId,
            SiteId = body.SiteId,
            StartDate = body.StartDate,
            EndDate = body.EndDate,
            VisitFrequencyPerYear = body.VisitFrequencyPerYear,
            Status = st,
            ContractValue = body.ContractValue,
        };
        _db.AMCContracts.Add(c);
        await _db.SaveChangesAsync(ct);
        var cust = await _db.Customers.AsNoTracking().FirstAsync(x => x.Id == c.CustomerId, ct);
        var site = await _db.Sites.AsNoTracking().FirstAsync(x => x.Id == c.SiteId, ct);
        return Ok(Map(c, cust.Name, site.Name));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AmcContractDto>> Update(Guid id, [FromBody] UpdateAmcContractRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var c = await _db.AMCContracts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
            return NotFound();
        if (!await OwnsCustomer(c.CustomerId, uid, ct))
            return Forbid();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        c.StartDate = body.StartDate;
        c.EndDate = body.EndDate;
        c.VisitFrequencyPerYear = body.VisitFrequencyPerYear;
        c.Status = st;
        c.ContractValue = body.ContractValue;
        await _db.SaveChangesAsync(ct);
        var cust = await _db.Customers.AsNoTracking().FirstAsync(x => x.Id == c.CustomerId, ct);
        var site = await _db.Sites.AsNoTracking().FirstAsync(x => x.Id == c.SiteId, ct);
        return Ok(Map(c, cust.Name, site.Name));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var c = await _db.AMCContracts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
            return NotFound();
        if (!await OwnsCustomer(c.CustomerId, uid, ct))
            return Forbid();
        _db.AMCContracts.Remove(c);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
