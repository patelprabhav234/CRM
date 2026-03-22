using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record AmcVisitDto(
    Guid Id,
    Guid AmcContractId,
    DateTimeOffset ScheduledDate,
    DateTimeOffset? CompletedDate,
    Guid? TechnicianUserId,
    string? TechnicianName,
    string Status);

public record CreateAmcVisitRequest(
    Guid AmcContractId,
    DateTimeOffset ScheduledDate,
    Guid? TechnicianUserId,
    string Status);

public record UpdateAmcVisitRequest(
    DateTimeOffset ScheduledDate,
    DateTimeOffset? CompletedDate,
    Guid? TechnicianUserId,
    string Status);

[ApiController]
[Authorize]
[Route("api/amc/visits")]
public class AmcVisitsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public AmcVisitsController(CrmDbContext db) => _db = db;

    private async Task<bool> CanAccessContract(Guid contractId, Guid userId, CancellationToken ct)
    {
        var c = await _db.AMCContracts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contractId, ct);
        if (c is null)
            return false;
        if (User.IsTenantAdminOrManager())
            return await _db.Customers.AnyAsync(x => x.Id == c.CustomerId, ct);
        return await _db.Customers.AnyAsync(x => x.Id == c.CustomerId && x.OwnerUserId == userId, ct);
    }

    private static bool TryParse(string s, out AMCVisitStatus st) => Enum.TryParse(s, ignoreCase: true, out st);

    private async Task<AmcVisitDto> MapVisit(AMCVisit v, CancellationToken ct)
    {
        string? tech = null;
        if (v.TechnicianUserId is { } tid)
            tech = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == tid, ct))?.Name;
        return new AmcVisitDto(v.Id, v.AMCContractId, v.ScheduledDate, v.CompletedDate, v.TechnicianUserId, tech, v.Status.ToString());
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AmcVisitDto>>> List([FromQuery] Guid? contractId, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var customerIds = User.IsTenantAdminOrManager()
            ? await _db.Customers.Select(c => c.Id).ToListAsync(ct)
            : await _db.Customers.Where(c => c.OwnerUserId == uid).Select(c => c.Id).ToListAsync(ct);
        var contractIds = await _db.AMCContracts
            .Where(c => customerIds.Contains(c.CustomerId))
            .Select(c => c.Id)
            .ToListAsync(ct);

        var q = _db.AMCVisits.AsNoTracking().Where(v => contractIds.Contains(v.AMCContractId));
        if (contractId is { } cid)
            q = q.Where(v => v.AMCContractId == cid);
        var rows = await q.OrderBy(v => v.ScheduledDate).ToListAsync(ct);
        var list = new List<AmcVisitDto>();
        foreach (var v in rows)
            list.Add(await MapVisit(v, ct));
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AmcVisitDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var v = await _db.AMCVisits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null)
            return NotFound();
        if (!await CanAccessContract(v.AMCContractId, uid, ct))
            return NotFound();
        return Ok(await MapVisit(v, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AmcVisitDto>> Create([FromBody] CreateAmcVisitRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (!await CanAccessContract(body.AmcContractId, uid, ct))
            return BadRequest("Contract not found.");
        if (body.TechnicianUserId is { } tid && !await _db.Users.AnyAsync(u => u.Id == tid, ct))
            return BadRequest("Technician not found.");

        var v = new AMCVisit
        {
            Id = Guid.NewGuid(),
            AMCContractId = body.AmcContractId,
            ScheduledDate = body.ScheduledDate,
            TechnicianUserId = body.TechnicianUserId,
            Status = st,
        };
        _db.AMCVisits.Add(v);
        await _db.SaveChangesAsync(ct);
        return Ok(await MapVisit(v, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AmcVisitDto>> Update(Guid id, [FromBody] UpdateAmcVisitRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var v = await _db.AMCVisits.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null)
            return NotFound();
        if (!await CanAccessContract(v.AMCContractId, uid, ct))
            return Forbid();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (body.TechnicianUserId is { } tid && !await _db.Users.AnyAsync(u => u.Id == tid, ct))
            return BadRequest("Technician not found.");

        v.ScheduledDate = body.ScheduledDate;
        v.CompletedDate = body.CompletedDate;
        v.TechnicianUserId = body.TechnicianUserId;
        v.Status = st;
        await _db.SaveChangesAsync(ct);
        return Ok(await MapVisit(v, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var v = await _db.AMCVisits.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null)
            return NotFound();
        if (!await CanAccessContract(v.AMCContractId, uid, ct))
            return Forbid();
        _db.AMCVisits.Remove(v);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
