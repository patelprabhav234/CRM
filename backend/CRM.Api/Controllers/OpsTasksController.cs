using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record OpsTaskDto(
    Guid Id,
    string Title,
    Guid AssignedToUserId,
    string? AssignedToName,
    DateTimeOffset DueDate,
    string Status,
    string TaskType,
    Guid? ServiceRequestId,
    Guid? AmcVisitId,
    Guid? InstallationJobId);

public record CreateOpsTaskRequest(
    string Title,
    Guid AssignedToUserId,
    DateTimeOffset DueDate,
    string Status,
    string TaskType,
    Guid? ServiceRequestId,
    Guid? AmcVisitId,
    Guid? InstallationJobId);

public record UpdateOpsTaskRequest(
    string Title,
    DateTimeOffset DueDate,
    string Status,
    string TaskType);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OpsTasksController : ControllerBase
{
    private readonly CrmDbContext _db;

    public OpsTasksController(CrmDbContext db) => _db = db;

    private static bool TryParseStatus(string s, out OpsTaskStatus st) => Enum.TryParse(s, ignoreCase: true, out st);
    private static bool TryParseType(string s, out OpsTaskType t) => Enum.TryParse(s, ignoreCase: true, out t);

    private async Task<OpsTaskDto> Map(OpsTask t, CancellationToken ct)
    {
        var name = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == t.AssignedToUserId, ct))?.Name;
        return new OpsTaskDto(
            t.Id,
            t.Title,
            t.AssignedToUserId,
            name,
            t.DueDate,
            t.Status.ToString(),
            t.TaskType.ToString(),
            t.ServiceRequestId,
            t.AMCVisitId,
            t.InstallationJobId);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OpsTaskDto>>> List([FromQuery] Guid? assignedTo, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var q = _db.OpsTasks.AsNoTracking().AsQueryable();
        if (assignedTo is { } aid)
            q = q.Where(t => t.AssignedToUserId == aid);
        else
            q = q.Where(t => t.AssignedToUserId == uid);

        var rows = await q.OrderBy(t => t.DueDate).ToListAsync(ct);
        var list = new List<OpsTaskDto>();
        foreach (var t in rows)
            list.Add(await Map(t, ct));
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OpsTaskDto>> Get(Guid id, CancellationToken ct)
    {
        var t = await _db.OpsTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
            return NotFound();
        return Ok(await Map(t, ct));
    }

    [HttpPost]
    public async Task<ActionResult<OpsTaskDto>> Create([FromBody] CreateOpsTaskRequest body, CancellationToken ct)
    {
        if (!TryParseStatus(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (!TryParseType(body.TaskType, out var tt))
            return BadRequest("Invalid task type.");
        if (!await _db.Users.AnyAsync(u => u.Id == body.AssignedToUserId, ct))
            return BadRequest("Assignee not found.");

        var task = new OpsTask
        {
            Id = Guid.NewGuid(),
            Title = body.Title.Trim(),
            AssignedToUserId = body.AssignedToUserId,
            DueDate = body.DueDate,
            Status = st,
            TaskType = tt,
            ServiceRequestId = body.ServiceRequestId,
            AMCVisitId = body.AmcVisitId,
            InstallationJobId = body.InstallationJobId,
        };
        _db.OpsTasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return Ok(await Map(task, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OpsTaskDto>> Update(Guid id, [FromBody] UpdateOpsTaskRequest body, CancellationToken ct)
    {
        var t = await _db.OpsTasks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
            return NotFound();
        if (!TryParseStatus(body.Status, out var st))
            return BadRequest("Invalid status.");
        if (!TryParseType(body.TaskType, out var tt))
            return BadRequest("Invalid task type.");

        t.Title = body.Title.Trim();
        t.DueDate = body.DueDate;
        t.Status = st;
        t.TaskType = tt;
        await _db.SaveChangesAsync(ct);
        return Ok(await Map(t, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var t = await _db.OpsTasks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
            return NotFound();
        _db.OpsTasks.Remove(t);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
