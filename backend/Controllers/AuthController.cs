using ITServiceManagement.API.Data;
using ITServiceManagement.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(ApplicationDbContext db) => _db = db;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var username = request.Username?.Trim() ?? "";
        var fullName = request.FullName?.Trim() ?? "";
        var email = request.Email?.Trim() ?? "";
        var department = request.Department?.Trim() ?? "";
        var role = string.IsNullOrWhiteSpace(request.Role) ? "Employee" : request.Role.Trim();

        if (username.Length < 3 || username.Length > 30)
            return BadRequest(new { success = false, message = "Username must be 3–30 characters." });
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { success = false, message = "Full name is required." });
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { success = false, message = "Please enter a valid email address." });
        if (request.Password.Length < 6 || request.Password.Length > 50)
            return BadRequest(new { success = false, message = "Password must be 6–50 characters." });
        if (string.IsNullOrWhiteSpace(department))
            return BadRequest(new { success = false, message = "Department is required." });

        if (await _db.Users.AnyAsync(x => x.Username.ToLower() == username.ToLower()))
            return Conflict(new { success = false, message = "Username already exists." });
        if (await _db.Users.AnyAsync(x => x.Email.ToLower() == email.ToLower()))
            return Conflict(new { success = false, message = "Email already exists." });

        var user = new User
        {
            Username = username,
            FullName = fullName,
            Email = email,
            Department = department,
            Role = role,
            IsActive = true
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Account successfully created.",
            username = user.Username
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var username = request.Username?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Username and password are required." });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower());
        if (user is null || !user.IsActive)
            return Unauthorized(new { success = false, message = "Invalid username or password." });

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { success = false, message = "Invalid username or password." });

        return Ok(new
        {
            success = true,
            message = "Login successful.",
            user = new
            {
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                department = user.Department,
                role = user.Role
            }
        });
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Department { get; set; } = "";
    public string Role { get; set; } = "Employee";
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
