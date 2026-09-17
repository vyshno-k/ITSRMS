using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.DTOs;
using ITServiceRequest.Api.Models;
using ITServiceRequest.Api.Services;
using ITServiceManagement.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
public class ServiceRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ServiceRequestService _service;
    private readonly HistoryService _history;
    private readonly SlaService _sla;
    private readonly ApplicationDbContext _employeeDb;

    public ServiceRequestsController(AppDbContext db, ServiceRequestService service, HistoryService history, SlaService sla, ApplicationDbContext employeeDb)
    {
        _db = db;
        _service = service;
        _history = history;
        _sla = sla;
        _employeeDb = employeeDb;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _employeeDb.Employees.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync();

        var requestEmployees = await _db.Employees.ToDictionaryAsync(x => x.Id);
        foreach (var employee in employees)
        {
            if (!requestEmployees.TryGetValue(employee.Id, out var requestEmployee))
            {
                _db.Employees.Add(new ITServiceRequest.Api.Models.Employee
                {
                    Id = employee.Id,
                    FullName = $"{employee.FirstName} {employee.LastName}".Trim(),
                    Email = employee.Email,
                    IsActive = employee.IsActive
                });
                continue;
            }

            requestEmployee.FullName = $"{employee.FirstName} {employee.LastName}".Trim();
            requestEmployee.Email = employee.Email;
            requestEmployee.IsActive = employee.IsActive;
        }

        await _db.SaveChangesAsync();

        return Ok(employees.Select(x => new { x.Id, FullName = $"{x.FirstName} {x.LastName}".Trim(), x.Email, x.IsActive }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestDto request)
    {
        if (request == null) return BadRequest("Request data is required.");
        if (request.EmployeeId <= 0) return BadRequest("Valid EmployeeId is required.");
        if (request.CategoryId <= 0) return BadRequest("Valid CategoryId is required.");
        if (request.ServiceTypeId <= 0) return BadRequest("Valid ServiceTypeId is required.");
        if (request.PriorityId <= 0) return BadRequest("Valid PriorityId is required.");
        if (string.IsNullOrWhiteSpace(request.Subject)) return BadRequest("Subject is required.");
        if (string.IsNullOrWhiteSpace(request.Description)) return BadRequest("Description is required.");

        if (!await _db.Employees.AnyAsync(x => x.Id == request.EmployeeId && x.IsActive))
            return BadRequest("Employee not found or inactive.");
        if (!await _db.Categories.AnyAsync(x => x.Id == request.CategoryId && x.IsActive))
            return BadRequest("Category not found or inactive.");
        if (!await _db.ServiceTypes.AnyAsync(x => x.Id == request.ServiceTypeId && x.IsActive))
            return BadRequest("Service type not found or inactive.");
        if (!await _db.Priorities.AnyAsync(x => x.Id == request.PriorityId))
            return BadRequest("Priority not found.");

        var ticket = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tickets = await BuildViewQuery().OrderByDescending(x => x.CreatedAt).ToListAsync();
        await EnrichSlaAsync(tickets);
        await EnrichResolutionAsync(tickets);
        return Ok(tickets);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? priorityId,
        [FromQuery] int? categoryId,
        [FromQuery] int? employeeId)
    {
        var query = BuildViewQuery();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.TicketNumber.Contains(search) || x.Subject.Contains(search) || x.Description.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (priorityId.HasValue) query = query.Where(x => x.PriorityId == priorityId.Value);
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        var tickets = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        await EnrichSlaAsync(tickets);
        await EnrichResolutionAsync(tickets);
        return Ok(tickets);
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetEmployeeHistory(int employeeId)
    {
        if (!await _db.Employees.AnyAsync(x => x.Id == employeeId))
            return NotFound($"Employee with ID {employeeId} was not found.");

        var tickets = await BuildViewQuery()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        await EnrichSlaAsync(tickets);
        await EnrichResolutionAsync(tickets);
        return Ok(tickets);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await BuildViewQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (ticket == null) return NotFound($"Service request with ID {id} was not found.");
        await EnrichSlaAsync(new List<ServiceRequestViewDto> { ticket });
        await EnrichResolutionAsync(new List<ServiceRequestViewDto> { ticket });
        return Ok(ticket);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceRequestDto request)
    {
        if (request == null) return BadRequest("Request data is required.");
        if (string.IsNullOrWhiteSpace(request.Subject)) return BadRequest("Subject is required.");
        if (string.IsNullOrWhiteSpace(request.Description)) return BadRequest("Description is required.");

        if (!await _db.Categories.AnyAsync(x => x.Id == request.CategoryId && x.IsActive)) return BadRequest("Category not found or inactive.");
        if (!await _db.ServiceTypes.AnyAsync(x => x.Id == request.ServiceTypeId && x.IsActive)) return BadRequest("Service type not found or inactive.");
        if (!await _db.Priorities.AnyAsync(x => x.Id == request.PriorityId)) return BadRequest("Priority not found.");

        var ticket = await _service.UpdateAsync(id, request, null);
        return ticket == null
            ? NotFound($"Service request with ID {id} was not found.")
            : Ok(ticket);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Status)) return BadRequest("Status is required.");
        var (success, error, ticket) = await _service.UpdateStatusAsync(id, request.Status.Trim(), request.PerformedBy, request.EngineerId);
        if (!success) return error == "not_found" ? NotFound($"Service request with ID {id} was not found.") : BadRequest(error);
        return Ok(ticket);
    }

    [HttpPost("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignTicketDto request)
    {
        if (request == null) return BadRequest("Assignment data is required.");
        if (request.EngineerId <= 0) return BadRequest("Valid EngineerId is required.");
        if (request.PerformedBy <= 0) return BadRequest("Valid PerformedBy employee ID is required.");

        var ticket = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (ticket == null) return NotFound($"Service request with ID {id} was not found.");

        var engineer = await _db.SupportEngineers.FirstOrDefaultAsync(e => e.Id == request.EngineerId);
        if (engineer == null) return BadRequest("Engineer not found.");
        if (!engineer.IsActive) return BadRequest("Cannot assign to an inactive engineer.");
        if (!await _db.Employees.AnyAsync(e => e.Id == request.PerformedBy && e.IsActive)) return BadRequest("Performed by employee not found or inactive.");

        var currentAssignments = await _db.TicketAssignments.Where(x => x.ServiceRequestId == id && x.IsCurrent).ToListAsync();
        foreach (var assignment in currentAssignments)
        {
            assignment.IsCurrent = false;
            assignment.UnassignedAt = DateTime.UtcNow;
        }

        var newAssignment = new TicketAssignment
        {
            ServiceRequestId = id,
            EngineerId = request.EngineerId,
            AssignedAt = DateTime.UtcNow,
            IsCurrent = true
        };
        _db.TicketAssignments.Add(newAssignment);

        var oldStatus = ticket.Status;
        if (ticket.Status == "New")
        {
            ticket.Status = "Assigned";
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _history.RecordAsync(id, "Ticket Assigned", null, $"Engineer {engineer.Id} - {engineer.FullName}", request.PerformedBy);
        if (oldStatus == "New") await _history.RecordAsync(id, "Status Changed", "New", "Assigned", request.PerformedBy);

        return Ok(new { assignmentId = newAssignment.Id, engineerId = engineer.Id, engineerName = engineer.FullName });
    }

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CommentText)) return BadRequest("CommentText is required.");
        if (!await _db.ServiceRequests.AnyAsync(x => x.Id == id)) return NotFound($"Service request with ID {id} was not found.");
        if (request.EmployeeId.HasValue && !await _db.Employees.AnyAsync(x => x.Id == request.EmployeeId.Value && x.IsActive)) return BadRequest("Comment employee not found or inactive.");

        var comment = new TicketComment
        {
            ServiceRequestId = id,
            EmployeeId = request.EmployeeId,
            CommentText = request.CommentText.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketComments.Add(comment);
        await _db.SaveChangesAsync();
        await _history.RecordAsync(id, "Comment Added", null, comment.CommentText, request.EmployeeId);
        return Ok(comment);
    }

    [HttpGet("{id:int}/comments")]
    public async Task<IActionResult> GetComments(int id)
    {
        if (!await _db.ServiceRequests.AnyAsync(x => x.Id == id)) return NotFound($"Service request with ID {id} was not found.");
        return Ok(await _db.TicketComments.AsNoTracking().Where(x => x.ServiceRequestId == id).OrderBy(x => x.CreatedAt).ToListAsync());
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveTicketDto request)
    {
        if (request == null) return BadRequest("Resolution data is required.");
        if (string.IsNullOrWhiteSpace(request.InvestigationNotes)) return BadRequest("Investigation notes are required.");
        if (string.IsNullOrWhiteSpace(request.ResolutionNotes)) return BadRequest("Resolution notes are required.");
        if (request.ResolvedBy <= 0) return BadRequest("Valid ResolvedBy employee ID is required.");
        if (request.EngineerId <= 0) return BadRequest("Valid EngineerId is required.");
        if (!await _db.Employees.AnyAsync(x => x.Id == request.ResolvedBy && x.IsActive)) return BadRequest("Resolved by employee not found or inactive.");

        var ticket = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (ticket == null) return NotFound($"Service request with ID {id} was not found.");
        if (ticket.Status != "In Progress") return BadRequest("Only In Progress tickets can be resolved.");

        var currentAssignment = await _db.TicketAssignments
            .AsNoTracking()
            .Where(x => x.ServiceRequestId == id && x.IsCurrent)
            .FirstOrDefaultAsync();
        if (currentAssignment == null) return BadRequest("Ticket must have an active engineer assignment before resolution.");
        if (currentAssignment.EngineerId != request.EngineerId) return BadRequest("Only the assigned support engineer can resolve this ticket.");

        var now = DateTime.UtcNow;
        var sla = await _sla.GetSlaStatusAsync(ticket);
        var slaResult = now <= (sla.ResolutionDueAt ?? DateTime.MaxValue) ? "Met" : "Breached";

        var resolution = new Resolution
        {
            ServiceRequestId = id,
            InvestigationNotes = request.InvestigationNotes.Trim(),
            ResolutionNotes = request.ResolutionNotes.Trim(),
            ResolvedBy = request.ResolvedBy,
            ResolvedAt = now,
            ResolutionDueAt = sla.ResolutionDueAt,
            SlaResult = slaResult
        };
        _db.Resolutions.Add(resolution);

        ticket.Status = "Resolved";
        ticket.ResolvedAt = now;
        ticket.UpdatedAt = now;
        await _db.SaveChangesAsync();
        await _history.RecordAsync(id, "Ticket Resolved", "In Progress", $"Resolved; SLA: {slaResult}; Resolution: {request.ResolutionNotes.Trim()}", request.ResolvedBy);
        return Ok(ticket);
    }

    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> Reopen(int id, [FromBody] ReopenTicketDto? request)
    {
        var ticket = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (ticket == null) return NotFound($"Service request with ID {id} was not found.");
        if (ticket.Status != "Resolved") return BadRequest("Only resolved tickets can be reopened.");

        var performedBy = request?.ReopenedBy > 0 ? request.ReopenedBy : (int?)null;
        if (performedBy.HasValue && !await _db.Employees.AnyAsync(x => x.Id == performedBy.Value && x.IsActive)) return BadRequest("Reopened by employee not found or inactive.");

        ticket.Status = "Reopened";
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.ResolvedAt = null;
        ticket.ClosedAt = null;
        await _db.SaveChangesAsync();
        var historyValue = string.IsNullOrWhiteSpace(request?.Reason) ? "Reopened" : $"Reopened: {request.Reason.Trim()}";
        await _history.RecordAsync(id, "Ticket Reopened", "Resolved", historyValue, performedBy);
        return Ok(ticket);
    }

    [HttpGet("{id:int}/resolution")]
    public async Task<IActionResult> GetLatestResolution(int id)
    {
        if (!await _db.ServiceRequests.AnyAsync(x => x.Id == id))
            return NotFound($"Service request with ID {id} was not found.");

        var resolution = await _db.Resolutions.AsNoTracking()
            .Where(x => x.ServiceRequestId == id)
            .OrderByDescending(x => x.ResolvedAt)
            .Select(x => new ResolutionViewDto
            {
                Id = x.Id,
                ServiceRequestId = x.ServiceRequestId,
                InvestigationNotes = x.InvestigationNotes,
                ResolutionNotes = x.ResolutionNotes,
                ResolvedBy = x.ResolvedBy,
                ResolvedAt = x.ResolvedAt,
                ResolutionDueAt = x.ResolutionDueAt,
                SlaResult = x.SlaResult
            })
            .FirstOrDefaultAsync();

        return Ok(resolution);
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        if (!await _db.ServiceRequests.AnyAsync(x => x.Id == id)) return NotFound($"Service request with ID {id} was not found.");
        return Ok(await _db.ServiceRequestHistories.AsNoTracking().Where(x => x.ServiceRequestId == id).OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    private async Task EnrichSlaAsync(List<ServiceRequestViewDto> tickets)
    {
        foreach (var item in tickets)
        {
            var entity = await _db.ServiceRequests.AsNoTracking().FirstAsync(x => x.Id == item.Id);
            var sla = await _sla.GetSlaStatusAsync(entity);
            item.EffectiveResponseDueAt = sla.ResponseDueAt;
            item.EffectiveResolutionDueAt = sla.ResolutionDueAt;
            item.ResponseSlaStatus = sla.ResponseSlaStatus;
            item.ResolutionSlaStatus = sla.ResolutionSlaStatus;
            item.IsSlaBreached = sla.IsBreached;
            item.IsSlaPaused = sla.IsPaused;
        }
    }

    private async Task EnrichResolutionAsync(List<ServiceRequestViewDto> tickets)
    {
        foreach (var item in tickets)
        {
            item.LatestResolution = await _db.Resolutions.AsNoTracking()
                .Where(x => x.ServiceRequestId == item.Id)
                .OrderByDescending(x => x.ResolvedAt)
                .Select(x => new ResolutionViewDto
                {
                    Id = x.Id,
                    ServiceRequestId = x.ServiceRequestId,
                    InvestigationNotes = x.InvestigationNotes,
                    ResolutionNotes = x.ResolutionNotes,
                    ResolvedBy = x.ResolvedBy,
                    ResolvedAt = x.ResolvedAt,
                    ResolutionDueAt = x.ResolutionDueAt,
                    SlaResult = x.SlaResult
                })
                .FirstOrDefaultAsync();
        }
    }

    private IQueryable<ServiceRequestViewDto> BuildViewQuery()
    {
        return from r in _db.ServiceRequests.AsNoTracking()
               join c in _db.Categories.AsNoTracking() on r.CategoryId equals c.Id into cg
               from c in cg.DefaultIfEmpty()
               join t in _db.ServiceTypes.AsNoTracking() on r.ServiceTypeId equals t.Id into tg
               from t in tg.DefaultIfEmpty()
               join p in _db.Priorities.AsNoTracking() on r.PriorityId equals p.Id into pg
               from p in pg.DefaultIfEmpty()
               let currentEngineerId = _db.TicketAssignments
                   .Where(a => a.ServiceRequestId == r.Id && a.IsCurrent)
                   .OrderByDescending(a => a.AssignedAt)
                   .Select(a => (int?)a.EngineerId)
                   .FirstOrDefault()
               let currentEngineerName = _db.SupportEngineers
                   .Where(e => e.Id == currentEngineerId)
                   .Select(e => e.FullName)
                   .FirstOrDefault()
               select new ServiceRequestViewDto
               {
                   Id = r.Id,
                   TicketNumber = r.TicketNumber,
                   EmployeeId = r.EmployeeId,
                   CategoryId = r.CategoryId,
                   CategoryName = c == null ? "" : c.Name,
                   ServiceTypeId = r.ServiceTypeId,
                   ServiceTypeName = t == null ? "" : t.Name,
                   PriorityId = r.PriorityId,
                   PriorityName = p == null ? "" : p.Name,
                   Subject = r.Subject,
                   Description = r.Description,
                   Status = r.Status,
                   CreatedAt = r.CreatedAt,
                   UpdatedAt = r.UpdatedAt,
                   ResponseDueAt = r.ResponseDueAt,
                   ResolutionDueAt = r.ResolutionDueAt,
                   ResolvedAt = r.ResolvedAt,
                   ClosedAt = r.ClosedAt,
                   EngineerId = currentEngineerId,
                   EngineerName = currentEngineerName
               };

}
}
