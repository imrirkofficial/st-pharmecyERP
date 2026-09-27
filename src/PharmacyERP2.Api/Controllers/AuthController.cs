using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PharmacyERP2.Application.DTOs;
using PharmacyERP2.Infrastructure.Data;

namespace PharmacyERP2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(PharmacyDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Username and password required.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username, ct);
        if (user is null || !user.IsActive)
            return Unauthorized("Invalid credentials.");

        bool ok;
        try
        {
            // BCrypt hash starts with $2; else treat as legacy plaintext (from old WinForms DB)
            ok = user.PasswordHash.StartsWith("$2")
                ? BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)
                : user.PasswordHash == req.Password;
        }
        catch { ok = false; }

        if (!ok)
            return Unauthorized("Invalid credentials.");

        // Auto-migrate legacy plaintext -> BCrypt
        if (!user.PasswordHash.StartsWith("$2"))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
            await db.SaveChangesAsync(ct);
        }

        user.LastLoginDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var jwt = config.GetSection("Jwt");
        var expiry = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"] ?? "480"));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
            ],
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenStr, user.Username, user.FullName, user.Role, expiry));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var username = User.Identity?.Name;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (user is null) return NotFound();
        return Ok(new MeResponse(user.Username, user.FullName, user.Role, user.Email));
    }
}
