using ITServiceManagement.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UsersController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Username, u.FullName, u.Email, u.Department, u.Role, u.IsActive })
            .ToListAsync();
        return Ok(users);
    }
}
