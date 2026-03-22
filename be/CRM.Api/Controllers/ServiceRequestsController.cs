using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record ServiceRequestDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? SiteId,
    string? SiteName,
    string Description,
    string Status,
    string? Priority,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTimeOffset CreatedAt);

public record CreateServiceRequestRequest(
    Guid CustomerId,
    Guid? SiteId,
    string Description,
    string Status,
    string? Priority,
    Guid? AssignedToUserId);

public record UpdateServiceRequestRequest(
    string Description,
    string Status,
    string? Priority,
    Guid? AssignedToUserId);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ServiceRequestsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public ServiceRequestsController(CrmDbContext db) => _db = db;

    private async Task<bool> OwnsCustomer(Guid customerId, Guid userId, CancellationToken ct) =>
        await _db.Customers.AnyAsync(c => c.Id == customerId && c.OwnerUserId == userId, ct);

    private static bool TryParse(string s, out ServiceRequestStatus st) => Enum.TryParse(s, ignoreCase: true, out st);

    private async Task<ServiceRequestDto> Map(ServiceRequest s, CancellationToken ct)
    {
        var cust = await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == s.CustomerId, ct);
        string? siteName = null;
        if (s.SiteId is { } sid)
            siteName = (await _db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sid, ct))?.Name;
        string? assignee = null;
        if (s.AssignedToUserId is { } aid)
            assignee = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == aid, ct))?.Name;
        return new ServiceRequestDto(
            s.Id,
            s.CustomerId,
            cust.Name,
            s.SiteId,
            siteName,
            s.Description,
            s.Status.ToString(),
            s.Priority,
            s.AssignedToUserId,
            assignee,
            s.CreatedAt);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestDto>>> List(CancellationToken ct)
    {
        var uid = User.GetUserId();
        var customerIds = await _db.Customers.Where(c => c.OwnerUserId == uid).Select(c => c.Id).ToListAsync(ct);
        var rows = await _db.ServiceRequests.AsNoTracking()
            .Where(s => customerIds.Contains(s.CustomerId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
        var list = new List<ServiceRequestDto>();
        foreach (var s in rows)
            list.Add(await Map(s, ct));
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceRequestDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var s = await _db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null)
            return NotFound();
        if (!await OwnsCustomer(s.CustomerId, uid, ct))
            return NotFound();
        return Ok(await Map(s, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ServiceRequestDto>> Create([FromBody] CreateServiceRequestRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (!await OwnsCustomer(body.CustomerId, uid, ct))
            return BadRequest("Customer not found.");
        if (body.SiteId is { } sid && !await _db.Sites.AnyAsync(s => s.Id == sid && s.CustomerId == body.CustomerId, ct))
            return BadRequest("Invalid site.");
        if (body.AssignedToUserId is { } aid && !await _db.Users.AnyAsync(u => u.Id == aid, ct))
            return BadRequest("User not found.");

        var s = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = body.CustomerId,
            SiteId = body.SiteId,
            Description = body.Description.Trim(),
            Status = st,
            Priority = string.IsNullOrWhiteSpace(body.Priority) ? null : body.Priority.Trim(),
            AssignedToUserId = body.AssignedToUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.ServiceRequests.Add(s);
        await _db.SaveChangesAsync(ct);
        return Ok(await Map(s, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceRequestDto>> Update(Guid id, [FromBody] UpdateServiceRequestRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var s = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null)
            return NotFound();
        if (!await OwnsCustomer(s.CustomerId, uid, ct))
            return Forbid();
        if (!TryParse(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (body.AssignedToUserId is { } aid && !await _db.Users.AnyAsync(u => u.Id == aid, ct))
            return BadRequest("User not found.");

        s.Description = body.Description.Trim();
        s.Status = st;
        s.Priority = string.IsNullOrWhiteSpace(body.Priority) ? null : body.Priority.Trim();
        s.AssignedToUserId = body.AssignedToUserId;
        await _db.SaveChangesAsync(ct);
        return Ok(await Map(s, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var s = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null)
            return NotFound();
        if (!await OwnsCustomer(s.CustomerId, uid, ct))
            return Forbid();
        _db.ServiceRequests.Remove(s);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
