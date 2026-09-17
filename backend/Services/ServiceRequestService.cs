using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.DTOs;
using ITServiceRequest.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Services;

public class ServiceRequestService
{
    private readonly AppDbContext _db;
    private readonly TicketNumberService _ticketNumber;
    private readonly HistoryService _history;
    private readonly SlaService _sla;

    public ServiceRequestService(AppDbContext db, TicketNumberService ticketNumber, HistoryService history, SlaService sla)
    {
        _db = db;
        _ticketNumber = ticketNumber;
        _history = history;
        _sla = sla;
    }

    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        ["New"] = new[] { "Assigned" },
        ["Assigned"] = new[] { "In Progress" },
        ["In Progress"] = new[] { "Resolved" },
        ["Resolved"] = new[] { "Closed", "Reopened" },
        ["Reopened"] = new[] { "In Progress" }
    };

    public bool IsValidStatusTransition(string currentStatus, string newStatus) =>
        AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);

    public async Task<ServiceRequest> CreateAsync(CreateServiceRequestDto request)
    {
        var ticket = new ServiceRequest
        {
            TicketNumber = _ticketNumber.Generate(),
            EmployeeId = request.EmployeeId,
            CategoryId = request.CategoryId,
            ServiceTypeId = request.ServiceTypeId,
            PriorityId = request.PriorityId,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Status = "New",
            CreatedAt = DateTime.UtcNow
        };

        var (responseDue, resolutionDue) = await _sla.CalculateDueTimesAsync(request.PriorityId, ticket.CreatedAt);
        ticket.ResponseDueAt = responseDue;
        ticket.ResolutionDueAt = resolutionDue;

        _db.ServiceRequests.Add(ticket);
        await _db.SaveChangesAsync();
        await _history.RecordAsync(ticket.Id, "Ticket Created", null, "New", request.EmployeeId);
        return ticket;
    }

    public async Task<ServiceRequest?> UpdateAsync(int id, UpdateServiceRequestDto request, int? performedBy = null)
    {
        var ticket = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (ticket == null) return null;
        if (ticket.Status == "Closed")
            throw new InvalidOperationException("Closed tickets cannot be edited except through authorized administrative actions.");

        var oldValue = $"Subject: {ticket.Subject}; Description: {ticket.Description}";
        ticket.CategoryId = request.CategoryId;
        ticket.ServiceTypeId = request.ServiceTypeId;
        ticket.PriorityId = request.PriorityId;
        var (responseDue, resolutionDue) = await _sla.CalculateDueTimesAsync(request.PriorityId, ticket.CreatedAt);
        ticket.ResponseDueAt = responseDue;
        ticket.ResolutionDueAt = resolutionDue;
        ticket.Subject = request.Subject.Trim();
        ticket.Description = request.Description.Trim();
        ticket.UpdatedAt = DateTime.UtcNow;

        await dbSaveAsync();
        await _history.RecordAsync(id, "Request Updated", oldValue, "Subject/Description/Category/Type/Priority updated", performedBy);
        return ticket;
    }

    public async Task<(bool success, string? error, ServiceRequest? ticket)> UpdateStatusAsync(
        int id, string newStatus, int? performedBy, int? performedByEngineerId = null)
    {
        var ticket = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (ticket == null) return (false, "not_found", null);
        var oldStatus = ticket.Status;

        if (!IsValidStatusTransition(oldStatus, newStatus))
            return (false, $"Cannot change status from '{oldStatus}' to '{newStatus}'.", null);

        if (newStatus == "Resolved")
            return (false, "Resolution notes are required. Use the Resolve action to mark the ticket Resolved.", null);

        if (newStatus == "Closed" && oldStatus != "Resolved")
            return (false, "A ticket can be closed only after it is Resolved.", null);

        if (newStatus == "In Progress")
        {
            var currentAssignment = await _db.TicketAssignments.FirstOrDefaultAsync(a => a.ServiceRequestId == id && a.IsCurrent);
            if (currentAssignment == null)
                return (false, "Ticket must be assigned to an engineer before it can move to In Progress.", null);
            if (performedByEngineerId.HasValue && currentAssignment.EngineerId != performedByEngineerId.Value)
                return (false, "Only the assigned support engineer can move this ticket to In Progress.", null);
            if (performedBy.HasValue && !await _db.Employees.AnyAsync(x => x.Id == performedBy.Value && x.IsActive))
                return (false, "Performed by employee not found or inactive.", null);
        }

        ticket.Status = newStatus;
        ticket.UpdatedAt = DateTime.UtcNow;
        if (newStatus == "Resolved") ticket.ResolvedAt = DateTime.UtcNow;
        if (newStatus == "Closed") ticket.ClosedAt = DateTime.UtcNow;

        await dbSaveAsync();
        await _history.RecordAsync(id, "Status Changed", oldStatus, newStatus, performedBy);
        return (true, null, ticket);
    }

    private async Task dbSaveAsync() => await _db.SaveChangesAsync();
}
