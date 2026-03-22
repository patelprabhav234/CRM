using CRM.Api.Extensions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record QuotationItemDto(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record QuotationDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? SiteId,
    string? SiteName,
    decimal TotalAmount,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<QuotationItemDto> Items);

public record QuotationItemLine(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateQuotationRequest(Guid CustomerId, Guid? SiteId, string Status, IReadOnlyList<QuotationItemLine> Items);

public record UpdateQuotationRequest(Guid? SiteId, string Status, IReadOnlyList<QuotationItemLine>? Items);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public QuotationsController(CrmDbContext db) => _db = db;

    private static bool TryParseStatus(string s, out QuotationStatus st) => Enum.TryParse(s, ignoreCase: true, out st);

    private async Task<QuotationDto?> LoadDto(Guid id, CancellationToken ct)
    {
        var q = await _db.Quotations.AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.Customer)
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null)
            return null;
        var items = q.Items.Select(i => new QuotationItemDto(
            i.Id,
            i.ProductId,
            i.Product.Name,
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice)).ToList();
        return new QuotationDto(
            q.Id,
            q.CustomerId,
            q.Customer.Name,
            q.SiteId,
            q.Site?.Name,
            q.TotalAmount,
            q.Status.ToString(),
            q.CreatedAt,
            items);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuotationDto>>> List(CancellationToken ct)
    {
        var uid = User.GetUserId();
        var ids = await _db.Quotations.AsNoTracking()
            .Where(q => q.OwnerUserId == uid)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => q.Id)
            .ToListAsync(ct);
        var list = new List<QuotationDto>();
        foreach (var id in ids)
        {
            var dto = await LoadDto(id, ct);
            if (dto != null)
                list.Add(dto);
        }
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var owns = await _db.Quotations.AnyAsync(q => q.Id == id && q.OwnerUserId == uid, ct);
        if (!owns)
            return NotFound();
        var dto = await LoadDto(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<QuotationDto>> Create([FromBody] CreateQuotationRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        if (!TryParseStatus(body.Status, out var status))
            return BadRequest("Invalid quotation status.");

        if (body.Items.Count == 0)
            return BadRequest("At least one line item is required.");

        if (!await _db.Customers.AnyAsync(c => c.Id == body.CustomerId && c.OwnerUserId == uid, ct))
            return BadRequest("Customer not found.");

        if (body.SiteId is { } sid)
        {
            var siteOk = await _db.Sites.AnyAsync(s => s.Id == sid && s.CustomerId == body.CustomerId, ct);
            if (!siteOk)
                return BadRequest("Site does not belong to customer.");
        }

        foreach (var line in body.Items)
        {
            if (!await _db.Products.AnyAsync(p => p.Id == line.ProductId && p.IsActive, ct))
                return BadRequest($"Product {line.ProductId} not found or inactive.");
        }

        var total = body.Items.Sum(i => i.Quantity * i.UnitPrice);
        var q = new Quotation
        {
            Id = Guid.NewGuid(),
            OwnerUserId = uid,
            CustomerId = body.CustomerId,
            SiteId = body.SiteId,
            TotalAmount = total,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Quotations.Add(q);
        foreach (var line in body.Items)
        {
            _db.QuotationItems.Add(new QuotationItem
            {
                Id = Guid.NewGuid(),
                QuotationId = q.Id,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
            });
        }
        await _db.SaveChangesAsync(ct);
        var dto = await LoadDto(q.Id, ct);
        return dto is null ? Problem("Failed to load quotation.") : Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> Update(Guid id, [FromBody] UpdateQuotationRequest body, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var q = await _db.Quotations.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == uid, ct);
        if (q is null)
            return NotFound();

        if (!TryParseStatus(body.Status, out var status))
            return BadRequest("Invalid status.");

        q.Status = status;
        q.SiteId = body.SiteId;
        if (body.SiteId is { } sid)
        {
            var siteOk = await _db.Sites.AnyAsync(s => s.Id == sid && s.CustomerId == q.CustomerId, ct);
            if (!siteOk)
                return BadRequest("Site does not belong to customer.");
        }

        if (body.Items is { } newItems && newItems.Count > 0)
        {
            _db.QuotationItems.RemoveRange(q.Items);
            foreach (var line in newItems)
            {
                if (!await _db.Products.AnyAsync(p => p.Id == line.ProductId, ct))
                    return BadRequest("Invalid product.");
                _db.QuotationItems.Add(new QuotationItem
                {
                    Id = Guid.NewGuid(),
                    QuotationId = q.Id,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                });
            }
            q.TotalAmount = newItems.Sum(i => i.Quantity * i.UnitPrice);
        }

        await _db.SaveChangesAsync(ct);
        var dto = await LoadDto(id, ct);
        return dto is null ? Problem() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = User.GetUserId();
        var q = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == uid, ct);
        if (q is null)
            return NotFound();
        _db.Quotations.Remove(q);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
