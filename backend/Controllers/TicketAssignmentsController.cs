using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.Models;
using ITServiceRequest.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Controllers;

[ApiController]
[Route("api/ticket-assignments")]
public class TicketAssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly HistoryService _history;

    public TicketAssignmentsController(AppDbContext db, HistoryService history)
    {
        _db = db;
        _history = history;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var assignments = await (
            from assignment in _db.TicketAssignments.AsNoTracking()
            join engineer in _db.SupportEngineers.AsNoTracking()
                on assignment.EngineerId equals engineer.Id
            orderby assignment.AssignedAt descending
            select new
            {
                assignment.Id,
                assignment.ServiceRequestId,
                SupportEngineerId = assignment.EngineerId,
                AssignedTo = engineer.FullName,
                assignment.AssignedAt,
                Status = assignment.IsCurrent ? "Assigned" : "Unassigned",
                SupportEngineer = engineer
            }).ToListAsync();

        return Ok(assignments);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AssignmentRequest request)
    {
        if (request.ServiceRequestId <= 0 || request.SupportEngineerId <= 0)
            return BadRequest("A valid service request and support engineer are required.");

        var ticket = await _db.ServiceRequests.FindAsync(request.ServiceRequestId);
        if (ticket is null) return NotFound("Service request was not found.");

        var engineer = await _db.SupportEngineers.FindAsync(request.SupportEngineerId);
        if (engineer is null) return NotFound("Support engineer was not found.");
        if (!engineer.IsActive) return BadRequest("Cannot assign to an inactive engineer.");

        var performedBy = await _db.Employees.Where(x => x.IsActive).Select(x => (int?)x.Id).FirstOrDefaultAsync();
        if (!performedBy.HasValue) return BadRequest("No active employee is available to record the assignment.");

        var current = await _db.TicketAssignments
            .Where(x => x.ServiceRequestId == request.ServiceRequestId && x.IsCurrent)
            .ToListAsync();
        foreach (var item in current)
        {
            item.IsCurrent = false;
            item.UnassignedAt = DateTime.UtcNow;
        }

        var assignment = new TicketAssignment
        {
            ServiceRequestId = request.ServiceRequestId,
            EngineerId = request.SupportEngineerId,
            AssignedAt = DateTime.UtcNow,
            IsCurrent = true
        };
        _db.TicketAssignments.Add(assignment);
        if (ticket.Status == "New")
        {
            ticket.Status = "Assigned";
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _history.RecordAsync(ticket.Id, "Ticket Assigned", null, $"Engineer {engineer.Id} - {engineer.FullName}", performedBy);
        return Ok(assignment);
    }
}

public class AssignmentRequest
{
    public int ServiceRequestId { get; set; }
    public int SupportEngineerId { get; set; }
}
