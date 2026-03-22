using System.Security.Claims;
using System.Text.RegularExpressions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Identity;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record RegisterTenantRequest(string CompanyName, string Subdomain, string Email, string Password, string Name);

public record LoginRequest(string Email, string Password, string TenantSubdomain);

public record AuthResponse(
    string Token,
    Guid UserId,
    Guid TenantId,
    string TenantSubdomain,
    string Email,
    string Name,
    string Role);

public record UserMeResponse(
    Guid UserId,
    Guid TenantId,
    string TenantSubdomain,
    string Email,
    string Name,
    string Role);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly Regex SubdomainRegex = new("^[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$", RegexOptions.Compiled);

    private readonly CrmDbContext _db;
    private readonly ITokenService _tokens;

    public AuthController(CrmDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    private static string NormalizeEmail(string s) => s.Trim().ToLowerInvariant();

    private static string NormalizeSubdomain(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;
        return s.Trim().ToLowerInvariant();
    }

    private static bool IsValidSubdomain(string s) =>
        s.Length >= 2 && s.Length <= 63 && SubdomainRegex.IsMatch(s);

    [HttpPost("register-tenant")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RegisterTenant([FromBody] RegisterTenantRequest body, CancellationToken ct)
    {
        var company = body.CompanyName.Trim();
        if (company.Length < 2 || company.Length > 300)
            return BadRequest("Company name must be between 2 and 300 characters.");

        var sub = NormalizeSubdomain(body.Subdomain);
        if (!IsValidSubdomain(sub))
            return BadRequest("Subdomain must be 2–63 characters: lowercase letters, digits, hyphens; not starting/ending with hyphen.");

        var email = NormalizeEmail(body.Email);
        if (string.IsNullOrWhiteSpace(body.Password) || body.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters.");

        if (await _db.Tenants.AnyAsync(t => t.Subdomain == sub, ct))
            return Conflict("That subdomain is already taken.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = company,
            Subdomain = sub,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Tenants.Add(tenant);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
            Name = body.Name.Trim(),
            Role = UserRole.Admin,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _tokens.CreateToken(user.Id, tenant.Id, user.Email, user.Name, user.Role);
        return Ok(new AuthResponse(token, user.Id, tenant.Id, tenant.Subdomain, user.Email, user.Name, user.Role.ToString()));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var email = NormalizeEmail(body.Email);
        var sub = NormalizeSubdomain(body.TenantSubdomain);
        if (string.IsNullOrEmpty(sub))
            return BadRequest("Tenant subdomain is required.");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == sub, ct);
        if (tenant is null || !tenant.IsActive)
            return Unauthorized("Unknown or inactive tenant.");

        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        var token = _tokens.CreateToken(user.Id, tenant.Id, user.Email, user.Name, user.Role);
        return Ok(new AuthResponse(token, user.Id, tenant.Id, tenant.Subdomain, user.Email, user.Name, user.Role.ToString()));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserMeResponse>> Me(CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Unauthorized();

        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == user.TenantId, ct);
        var sub = tenant?.Subdomain ?? string.Empty;

        return Ok(new UserMeResponse(user.Id, user.TenantId, sub, user.Email, user.Name, user.Role.ToString()));
    }
}
