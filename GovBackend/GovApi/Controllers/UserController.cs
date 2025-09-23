using System.ComponentModel.DataAnnotations;
using BCrypt.Net;
using GovApi.Contracts;
using GovApi.Data;
using GovApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GovApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // POST: /api/users/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email and password are required.");

        if (!new EmailAddressAttribute().IsValid(req.Email))
            return BadRequest("Invalid email format.");

        var emailNorm = req.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm);
        if (exists)
            return Conflict("Email already registered.");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12);

        var user = new User
        {
            Email = emailNorm,
            PasswordHash = hash,
            SearchCredits = 3,
            LastReset = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = new UserDto(user.Id, user.Email, user.SearchCredits, user.LastReset);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, dto);
    }

// POST: /api/users/login
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest req)
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return BadRequest("Email and password are required.");

    var emailNorm = req.Email.Trim().ToLowerInvariant();

    var user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == emailNorm);
    if (user == null)
        return Unauthorized("Invalid credentials.");

    var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
    if (!ok)
        return Unauthorized("Invalid credentials.");

    // Monthly credit reset
    if ((DateTime.UtcNow - user.LastReset).TotalDays >= 30)
    {
        user.SearchCredits = 3;
        user.LastReset = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // 🔑 Generate JWT
    var jwtConfig = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email)
    };

    var token = new JwtSecurityToken(
        issuer: jwtConfig["Issuer"],
        audience: jwtConfig["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    // ✅ Return token instead of user dto
    return Ok(new { token = tokenString });
}


    // GET: /api/users/{id}  (helper for CreatedAtAction)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        return new UserDto(user.Id, user.Email, user.SearchCredits, user.LastReset);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(int.Parse(userId));
        if (user == null) return NotFound();

        return new UserDto(user.Id, user.Email, user.SearchCredits, user.LastReset);
    }
}

