using System.ComponentModel.DataAnnotations;        // For email validation attribute
using BCrypt.Net;                                  // For hashing & verifying passwords
using GovApi.Contracts;                            // DTOs (RegisterRequest, LoginRequest, UserDto)
using GovApi.Data;                                 // AppDbContext (EF Core DB context)
using GovApi.Models;                               // User entity model
using Microsoft.AspNetCore.Mvc;                    // For API controllers
using Microsoft.EntityFrameworkCore;               // EF Core methods (AnyAsync, SingleOrDefaultAsync, etc.)
using Microsoft.AspNetCore.Authorization;          // For [Authorize] attribute
using System.Security.Claims;                      // For working with claims from JWT
using System.IdentityModel.Tokens.Jwt;             // For creating and parsing JWTs
using Microsoft.IdentityModel.Tokens;              // For signing JWTs
using System.Text;                                 // For Encoding.UTF8 (needed for secret key bytes)

namespace GovApi.Controllers;

// Marks this class as an API controller
// Route will be: /api/users (because of [controller] token)
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    // Dependency injection: AppDbContext is passed in automatically
    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // -----------------------
    // POST: /api/users/register
    // Creates a new user account
    // -----------------------
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        // Basic validation: make sure fields aren’t empty
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email and password are required.");

        // Validate email format
        if (!new EmailAddressAttribute().IsValid(req.Email))
            return BadRequest("Invalid email format.");

        // Normalize email (lowercase, trim spaces)
        var emailNorm = req.Email.Trim().ToLowerInvariant();

        // Check if email already exists in DB
        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm);
        if (exists)
            return Conflict("Email already registered.");

        // Hash password with BCrypt before storing
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12);

        // Create new user object
        var user = new User
        {
            Email = emailNorm,
            PasswordHash = hash,
            SearchCredits = 3,           // Give 3 free monthly credits
            LastReset = DateTime.UtcNow  // Track when credits last reset
        };

        // Save to database
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Map to safe DTO (don’t return password hash)
        var dto = new UserDto(user.Id, user.Email, user.SearchCredits, user.LastReset);

        // Return 201 Created with user info
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, dto);
    }

    // -----------------------
    // POST: /api/users/login
    // Logs in a user and issues a JWT
    // -----------------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email and password are required.");

        var emailNorm = req.Email.Trim().ToLowerInvariant();

        // Find user by email
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == emailNorm);
        if (user == null)
            return Unauthorized("Invalid credentials.");

        // Verify password hash
        var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!ok)
            return Unauthorized("Invalid credentials.");

        // Reset credits if a month has passed
        if ((DateTime.UtcNow - user.LastReset).TotalDays >= 30)
        {
            user.SearchCredits = 3;
            user.LastReset = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // -----------------------
        // Generate JWT token
        // -----------------------
        var jwtConfig = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");

        // Get secret key from config
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Add claims (data inside the token)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // User ID
            new Claim(JwtRegisteredClaimNames.Email, user.Email)        // User email
        };

        // Create token object
        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
            signingCredentials: creds
        );

        // Convert token to string
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Return token to client
        return Ok(new { token = tokenString });
    }

    // -----------------------
    // GET: /api/users/{id}
    // Get user by ID (not usually used in production, but good for testing)
    // -----------------------
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        return new UserDto(user.Id, user.Email, user.SearchCredits, user.LastReset);
    }

    // -----------------------
    // GET: /api/users/me
    // Get the current logged-in user (requires JWT in Authorization header)
    // -----------------------
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        // Log all claims (for debugging)
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"{claim.Type} = {claim.Value}");
        }

        // Use NameIdentifier instead of Sub
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(int.Parse(userId));
        if (user == null) return NotFound();

        return new UserDto(user.Id, user.Email, user.SearchCredits, user.LastReset);
    }
}
