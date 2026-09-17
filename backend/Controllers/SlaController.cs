using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.DTOs;
using ITServiceRequest.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Controllers;

[ApiController]
[Route("api/sla")]
public class SlaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SlaService _sla;

    public SlaController(AppDbContext db, SlaService sla)
    {
        _db = db;
        _sla = sla;
    }

    [HttpGet("configurations")]
    public async Task<IActionResult> GetConfigurations() =>
        Ok(await _sla.GetConfigurationsAsync());

    [HttpPut("configurations")]
    public async Task<IActionResult> SaveConfigurations([FromBody] List<UpdateSlaConfigurationDto> requests)
    {
        if (requests == null || requests.Count == 0) return BadRequest("At least one SLA configuration is required.");
        foreach (var item in requests)
        {
            if (item.PriorityId <= 0) return BadRequest("Valid PriorityId is required.");
            if (item.ResponseTargetMinutes <= 0) return BadRequest("Response target must be greater than zero.");
            if (item.ResolutionTargetMinutes < 0 || item.ResolutionTargetBusinessDays < 0) return BadRequest("Resolution targets cannot be negative.");
            if (item.ResolutionTargetMinutes == 0 && item.ResolutionTargetBusinessDays == 0) return BadRequest("A resolution target is required.");
        }

        var priorityIds = await _db.Priorities.Select(x => x.Id).ToListAsync();
        if (requests.Select(x => x.PriorityId).Distinct().Count() != requests.Count || requests.Any(x => !priorityIds.Contains(x.PriorityId)))
            return BadRequest("Each configured priority must exist and appear only once.");

        await _sla.SaveConfigurationsAsync(requests);
        return Ok(await _sla.GetConfigurationsAsync());
    }

    [HttpGet("pause-statuses")]
    public async Task<IActionResult> GetPauseStatuses() => Ok(await _sla.GetPauseStatusesAsync());

    [HttpPut("pause-statuses")]
    public async Task<IActionResult> SavePauseStatuses([FromBody] UpdateSlaPauseStatusesDto request)
    {
        if (request == null) return BadRequest("Pause status data is required.");
        await _sla.SavePauseStatusesAsync(request.Statuses ?? []);
        return Ok(await _sla.GetPauseStatusesAsync());
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] string? filter = null)
    {
        var tickets = await _sla.GetSlaTicketsAsync(filter);
        return Ok(tickets);
    }

    [HttpGet("tickets/{id:int}")]
    public async Task<IActionResult> GetTicket(int id)
    {
        var ticket = await _db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (ticket == null) return NotFound($"Service request with ID {id} was not found.");
        return Ok(await _sla.GetSlaStatusAsync(ticket));
    }
}
