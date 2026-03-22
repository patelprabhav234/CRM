using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record CustomerDto(Guid Id, int SerialId, string Name, string? ContactPerson, string? Phone, string? Email, string? Address, DateTimeOffset CreatedAt);
public record CustomerDetailDto(Guid Id, int SerialId, string Name, string? ContactPerson, string? Phone, string? Email, string? Address, DateTimeOffset CreatedAt, IReadOnlyList<SiteListItemDto> Sites);
public record SiteListItemDto(Guid Id, int SerialId, string Name, string? City, string? State, string SiteType, string? ComplianceStatus);

public record CreateCustomerRequest(string Name, string? ContactPerson, string? Phone, string? Email, string? Address);
public record UpdateCustomerRequest(string Name, string? ContactPerson, string? Phone, string? Email, string? Address);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CrmDbContext _db;

    public CustomersController(CrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(CancellationToken ct)
    {
        var uid = User.GetUserId();
        var rows = await _db.Customers.AsNoTracking()
            .Where(c => c.OwnerUserId == uid)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        return Ok(rows.Select(c => new CustomerDto(c.Id, c.SerialId, c.Name, c.ContactPerson, c.Phone, c.Email, c.Address, c.CreatedAt)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var c = await _db.Customers.AsNoTracking()
            .Include(x => x.Sites)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == uid, ct);
        if (c is null)
            return NotFound();
        var sites = c.Sites.OrderBy(s => s.Name).Select(s => new SiteListItemDto(s.Id, s.SerialId, s.Name, s.City, s.State, s.SiteType.ToString(), s.ComplianceStatus)).ToList();
        return Ok(new CustomerDetailDto(c.Id, c.SerialId, c.Name, c.ContactPerson, c.Phone, c.Email, c.Address, c.CreatedAt, sites));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var c = new Customer
        {
            Id = Guid.NewGuid(),
            OwnerUserId = uid,
            Name = body.Name.Trim(),
            ContactPerson = Trim(body.ContactPerson),
            Phone = Trim(body.Phone),
            Email = Trim(body.Email),
            Address = Trim(body.Address),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Customers.Add(c);
        await _db.SaveChangesAsync(ct);
        return Ok(new CustomerDto(c.Id, c.SerialId, c.Name, c.ContactPerson, c.Phone, c.Email, c.Address, c.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, [FromBody] UpdateCustomerRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == uid, ct);
        if (c is null)
            return NotFound();
        c.Name = body.Name.Trim();
        c.ContactPerson = Trim(body.ContactPerson);
        c.Phone = Trim(body.Phone);
        c.Email = Trim(body.Email);
        c.Address = Trim(body.Address);
        await _db.SaveChangesAsync(ct);
        return Ok(new CustomerDto(c.Id, c.SerialId, c.Name, c.ContactPerson, c.Phone, c.Email, c.Address, c.CreatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == uid, ct);
        if (c is null)
            return NotFound();
        _db.Customers.Remove(c);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
