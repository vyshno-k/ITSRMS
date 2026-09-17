using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Controllers;

[ApiController]
[Route("api/support-engineers")]
public class SupportEngineersController : ControllerBase
{
    private readonly AppDbContext _db;
    public SupportEngineersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.SupportEngineers.AsNoTracking().OrderBy(x => x.FullName).ToListAsync());

    [HttpGet("workload")]
    public async Task<IActionResult> GetWorkload()
    {
        var workload = await _db.SupportEngineers.AsNoTracking()
            .Select(engineer => new
            {
                engineer.Id,
                engineer.FullName,
                engineer.Email,
                engineer.IsActive,
                AssignedTickets = _db.TicketAssignments.Count(assignment =>
                    assignment.EngineerId == engineer.Id && assignment.IsCurrent)
            })
            .OrderByDescending(x => x.AssignedTickets)
            .ThenBy(x => x.FullName)
            .ToListAsync();

        return Ok(workload);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupportEngineer engineer)
    {
        if (string.IsNullOrWhiteSpace(engineer.FullName) || string.IsNullOrWhiteSpace(engineer.Email))
            return BadRequest("FullName and Email are required.");

        engineer.Id = 0;
        engineer.FullName = engineer.FullName.Trim();
        engineer.Email = engineer.Email.Trim();
        _db.SupportEngineers.Add(engineer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = engineer.Id }, engineer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupportEngineer input)
    {
        var engineer = await _db.SupportEngineers.FindAsync(id);
        if (engineer is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Email))
            return BadRequest("FullName and Email are required.");

        engineer.FullName = input.FullName.Trim();
        engineer.Email = input.Email.Trim();
        engineer.IsActive = input.IsActive;
        await _db.SaveChangesAsync();
        return Ok(engineer);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var engineer = await _db.SupportEngineers.FindAsync(id);
        if (engineer is null) return NotFound();
        engineer.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
