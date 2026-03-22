using System.Security.Claims;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Identity;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, Guid UserId, string Email, string Name, string Role);
public record UserMeResponse(Guid UserId, string Email, string Name, string Role);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CrmDbContext _db;
    private readonly ITokenService _tokens;

    public AuthController(CrmDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest body, CancellationToken ct)
    {
        var email = body.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(body.Password) || body.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters.");
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict("Email already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
            Name = body.Name.Trim(),
            Role = UserRole.Sales,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _tokens.CreateToken(user.Id, user.Email, user.Name, user.Role);
        return Ok(new AuthResponse(token, user.Id, user.Email, user.Name, user.Role.ToString()));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var email = body.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        var token = _tokens.CreateToken(user.Id, user.Email, user.Name, user.Role);
        return Ok(new AuthResponse(token, user.Id, user.Email, user.Name, user.Role.ToString()));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserMeResponse>> Me(CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Unauthorized();
        return Ok(new UserMeResponse(user.Id, user.Email, user.Name, user.Role.ToString()));
    }
}
