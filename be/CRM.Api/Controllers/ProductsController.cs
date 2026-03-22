using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record ProductDto(Guid Id, string Name, string Category, decimal Price, string? Description, bool IsActive);

public record CreateProductRequest(string Name, string Category, decimal Price, string? Description, bool IsActive);
public record UpdateProductRequest(string Name, string Category, decimal Price, string? Description, bool IsActive);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly CrmDbContext _db;

    public ProductsController(CrmDbContext db) => _db = db;

    private static ProductDto Map(Product p) =>
        new(p.Id, p.Name, p.Category, p.Price, p.Description, p.IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List(CancellationToken ct)
    {
        var rows = await _db.Products.AsNoTracking().OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);
        return Ok(rows.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
            return NotFound();
        return Ok(Map(p));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest body, CancellationToken ct)
    {
        var p = new Product
        {
            Id = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Category = body.Category.Trim(),
            Price = body.Price,
            Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim(),
            IsActive = body.IsActive,
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync(ct);
        return Ok(Map(p));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductRequest body, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
            return NotFound();
        p.Name = body.Name.Trim();
        p.Category = body.Category.Trim();
        p.Price = body.Price;
        p.Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim();
        p.IsActive = body.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(Map(p));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
            return NotFound();
        _db.Products.Remove(p);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
