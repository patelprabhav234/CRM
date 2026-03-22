using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record LeadDto(
    Guid Id,
    int SerialId,
    string Name,
    string? Company,
    string? Email,
    string? Phone,
    string? Location,
    string? City,
    string? State,
    string? Requirement,
    string Source,
    string Status,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateLeadRequest(
    string Name,
    string? Company,
    string? Email,
    string? Phone,
    string? Location,
    string? City,
    string? State,
    string? Requirement,
    string Source,
    string Status,
    Guid? AssignedToUserId);

public record UpdateLeadRequest(
    string Name,
    string? Company,
    string? Email,
    string? Phone,
    string? Location,
    string? City,
    string? State,
    string? Requirement,
    string Source,
    string Status,
    Guid? AssignedToUserId);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public LeadsController(CrmDbContext db) => _db = db;

    private static LeadDto Map(Lead x) => new(
        x.Id,
        x.SerialId,
        x.Name,
        x.Company,
        x.Email,
        x.Phone,
        x.Location,
        x.City,
        x.State,
        x.Requirement,
        x.Source,
        x.Status.ToString(),
        x.AssignedToUserId,
        x.CreatedAt,
        x.UpdatedAt);

    private static bool TryParseStatus(string s, out LeadStatus st) =>
        Enum.TryParse(s, ignoreCase: true, out st);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeadDto>>> List(CancellationToken ct)
    {
        var uid = User.GetUserId();
        var q = _db.Leads.AsNoTracking();
        if (!User.IsTenantAdminOrManager())
            q = q.Where(x => x.OwnerUserId == uid);
        var items = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Ok(items.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var q = _db.Leads.AsNoTracking().Where(l => l.Id == id);
        if (!User.IsTenantAdminOrManager())
            q = q.Where(l => l.OwnerUserId == uid);
        var x = await q.FirstOrDefaultAsync(ct);
        if (x is null)
            return NotFound();
        return Ok(Map(x));
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create([FromBody] CreateLeadRequest body, CancellationToken ct)
    {
        if (!TryParseStatus(body.Status, out var status))
            return BadRequest("Invalid status.");

        var uid = User.GetUserId();
        if (body.AssignedToUserId is { } aid && !await _db.Users.AnyAsync(u => u.Id == aid, ct))
            return BadRequest("Assigned user not found.");

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            OwnerUserId = uid,
            Name = body.Name.Trim(),
            Company = NullIfEmpty(body.Company),
            Email = NullIfEmpty(body.Email),
            Phone = NullIfEmpty(body.Phone),
            Location = NullIfEmpty(body.Location),
            City = NullIfEmpty(body.City),
            State = NullIfEmpty(body.State),
            Requirement = NullIfEmpty(body.Requirement),
            Source = string.IsNullOrWhiteSpace(body.Source) ? "Call" : body.Source.Trim(),
            Status = status,
            AssignedToUserId = body.AssignedToUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = lead.Id }, Map(lead));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeadDto>> Update(Guid id, [FromBody] UpdateLeadRequest body, CancellationToken ct)
    {
        if (!TryParseStatus(body.Status, out var status))
            return BadRequest("Invalid status.");

        var uid = User.GetUserId();
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return NotFound();
        if (!User.IsTenantAdminOrManager() && lead.OwnerUserId != uid)
            return NotFound();

        if (body.AssignedToUserId is { } aid && !await _db.Users.AnyAsync(u => u.Id == aid, ct))
            return BadRequest("Assigned user not found.");

        lead.Name = body.Name.Trim();
        lead.Company = NullIfEmpty(body.Company);
        lead.Email = NullIfEmpty(body.Email);
        lead.Phone = NullIfEmpty(body.Phone);
        lead.Location = NullIfEmpty(body.Location);
        lead.City = NullIfEmpty(body.City);
        lead.State = NullIfEmpty(body.State);
        lead.Requirement = NullIfEmpty(body.Requirement);
        lead.Source = string.IsNullOrWhiteSpace(body.Source) ? "Call" : body.Source.Trim();
        lead.Status = status;
        lead.AssignedToUserId = body.AssignedToUserId;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(Map(lead));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return NotFound();
        if (!User.IsTenantAdminOrManager() && lead.OwnerUserId != uid)
            return NotFound();
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
