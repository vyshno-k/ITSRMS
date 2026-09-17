using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.DTOs;
using ITServiceRequest.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Services;

public class SlaService
{
    private readonly AppDbContext _db;

    public SlaService(AppDbContext db) => _db = db;

    public async Task<(DateTime? responseDue, DateTime? resolutionDue)> CalculateDueTimesAsync(int priorityId, DateTime createdAt)
    {
        var config = await _db.SlaConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.PriorityId == priorityId);
        if (config == null)
        {
            config = DefaultConfiguration(priorityId);
        }

        var responseDue = AddCalendarMinutes(createdAt, config.ResponseTargetMinutes);
        var resolutionDue = config.ResolutionTargetBusinessDays > 0
            ? AddBusinessDays(createdAt, config.ResolutionTargetBusinessDays)
            : AddCalendarMinutes(createdAt, config.ResolutionTargetMinutes);

        return (responseDue, resolutionDue);
    }

    public async Task<List<SlaConfigurationDto>> GetConfigurationsAsync()
    {
        return await (from c in _db.SlaConfigurations.AsNoTracking()
                      join p in _db.Priorities.AsNoTracking() on c.PriorityId equals p.Id
                      orderby p.Level
                      select new SlaConfigurationDto
                      {
                          Id = c.Id,
                          PriorityId = c.PriorityId,
                          PriorityName = p.Name,
                          ResponseTargetMinutes = c.ResponseTargetMinutes,
                          ResolutionTargetMinutes = c.ResolutionTargetMinutes,
                          ResolutionTargetBusinessDays = c.ResolutionTargetBusinessDays
                      }).ToListAsync();
    }

    public async Task SaveConfigurationsAsync(IEnumerable<UpdateSlaConfigurationDto> requests)
    {
        var existing = await _db.SlaConfigurations.ToListAsync();
        foreach (var item in requests)
        {
            var config = existing.FirstOrDefault(x => x.PriorityId == item.PriorityId);
            if (config == null)
            {
                config = new SlaConfiguration { PriorityId = item.PriorityId };
                _db.SlaConfigurations.Add(config);
            }
            config.ResponseTargetMinutes = item.ResponseTargetMinutes;
            config.ResolutionTargetMinutes = item.ResolutionTargetMinutes;
            config.ResolutionTargetBusinessDays = item.ResolutionTargetBusinessDays;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetPauseStatusesAsync()
    {
        var allowed = new[] { "New", "Assigned", "In Progress", "Resolved", "Closed" };
        var order = allowed.Select((status, index) => new { status, index }).ToDictionary(x => x.status, x => x.index);

        return await _db.SlaPauseStatuses.AsNoTracking()
            .Where(x => x.IsActive && allowed.Contains(x.Status))
            .Select(x => x.Status)
            .ToListAsync()
            .ContinueWith(task => task.Result
                .Distinct()
                .OrderBy(status => order.TryGetValue(status, out var index) ? index : int.MaxValue)
                .ToList());
    }

    public async Task SavePauseStatusesAsync(IEnumerable<string> statuses)
    {
        var allowed = new[] { "New", "Assigned", "In Progress", "Resolved", "Closed" };
        var selected = statuses
            .Select(x => x?.Trim() ?? "")
            .Where(x => allowed.Contains(x))
            .Distinct()
            .ToHashSet();

        var existing = await _db.SlaPauseStatuses.ToListAsync();
        foreach (var item in existing)
        {
            item.IsActive = selected.Contains(item.Status);
            if (!allowed.Contains(item.Status))
                item.IsActive = false;
        }

        foreach (var status in selected.Where(x => existing.All(e => e.Status != x)))
            _db.SlaPauseStatuses.Add(new SlaPauseStatus { Status = status, IsActive = true });

        foreach (var stale in existing.Where(x => !allowed.Contains(x.Status)))
            stale.IsActive = false;

        await _db.SaveChangesAsync();
    }

    public async Task<SlaStatusDto> GetSlaStatusAsync(ServiceRequest ticket)
    {
        var config = await _db.SlaConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.PriorityId == ticket.PriorityId)
                     ?? DefaultConfiguration(ticket.PriorityId);
        var pauseStatuses = await GetPauseStatusesAsync();
        var histories = await _db.ServiceRequestHistories.AsNoTracking()
            .Where(x => x.ServiceRequestId == ticket.Id && x.Action == "Status Changed")
            .OrderBy(x => x.CreatedAt).ToListAsync();

        var responseDue = ticket.ResponseDueAt ?? AddCalendarMinutes(ticket.CreatedAt, config.ResponseTargetMinutes);
        var resolutionDue = ticket.ResolutionDueAt ?? (config.ResolutionTargetBusinessDays > 0
            ? AddBusinessDays(ticket.CreatedAt, config.ResolutionTargetBusinessDays)
            : AddCalendarMinutes(ticket.CreatedAt, config.ResolutionTargetMinutes));

        responseDue = ShiftDueForPauses(ticket.CreatedAt, ticket.Status, responseDue, histories, pauseStatuses);
        resolutionDue = ShiftDueForPauses(ticket.CreatedAt, ticket.Status, resolutionDue, histories, pauseStatuses);

        var isPaused = pauseStatuses.Contains(ticket.Status);

        var now = DateTime.UtcNow;
        var responseStatus = ticket.ResolvedAt.HasValue
            ? (ticket.ResolvedAt.Value <= responseDue ? "Met" : "Breached")
            : (isPaused ? "Paused" : (now > responseDue ? "Breached" : "On Track"));

        var resolutionEnd = ticket.ResolvedAt ?? ticket.ClosedAt;
        var resolutionStatus = resolutionEnd.HasValue
            ? (resolutionEnd.Value <= resolutionDue ? "Met" : "Breached")
            : (isPaused ? "Paused" : (now > resolutionDue ? "Breached" : "On Track"));

        return new SlaStatusDto
        {
            ResponseDueAt = responseDue,
            ResolutionDueAt = resolutionDue,
            ResponseSlaStatus = responseStatus,
            ResolutionSlaStatus = resolutionStatus,
            IsBreached = responseStatus == "Breached" || resolutionStatus == "Breached",
            IsPaused = isPaused
        };
    }

    public async Task<List<SlaTicketDto>> GetSlaTicketsAsync(string? filter)
    {
        var tickets = await (from r in _db.ServiceRequests.AsNoTracking()
                              join p in _db.Priorities.AsNoTracking() on r.PriorityId equals p.Id
                              let engineerId = _db.TicketAssignments.Where(a => a.ServiceRequestId == r.Id && a.IsCurrent).Select(a => (int?)a.EngineerId).FirstOrDefault()
                              let engineerName = _db.SupportEngineers.Where(e => e.Id == engineerId).Select(e => e.FullName).FirstOrDefault()
                              orderby r.CreatedAt descending
                              select new { r, p.Name, EngineerId = engineerId, EngineerName = engineerName }).ToListAsync();

        var result = new List<SlaTicketDto>();
        foreach (var item in tickets)
        {
            var status = await GetSlaStatusAsync(item.r);
            result.Add(new SlaTicketDto
            {
                Id = item.r.Id, TicketNumber = item.r.TicketNumber, PriorityId = item.r.PriorityId, PriorityName = item.Name,
                Status = item.r.Status, CreatedAt = item.r.CreatedAt, ResponseDueAt = status.ResponseDueAt, ResolutionDueAt = status.ResolutionDueAt,
                ResponseSlaStatus = status.ResponseSlaStatus, ResolutionSlaStatus = status.ResolutionSlaStatus, IsBreached = status.IsBreached, IsPaused = status.IsPaused,
                EngineerId = item.EngineerId, EngineerName = item.EngineerName, Subject = item.r.Subject
            });
        }

        var normalized = filter?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized) || normalized == "all") return result;

        if (normalized == "on track") normalized = "ontrack";

        return normalized switch
        {
            "breached" => result.Where(x => GetOverallSlaCategory(x) == "breached").ToList(),
            "paused" => result.Where(x => GetOverallSlaCategory(x) == "paused").ToList(),
            "ontrack" => result.Where(x => GetOverallSlaCategory(x) == "ontrack").ToList(),
            _ => result
        };
    }

    private static string GetOverallSlaCategory(SlaTicketDto ticket)
    {
        if (ticket.ResponseSlaStatus == "Breached" || ticket.ResolutionSlaStatus == "Breached") return "breached";
        if (ticket.ResponseSlaStatus == "Paused" || ticket.ResolutionSlaStatus == "Paused") return "paused";

        var responseHealthy = ticket.ResponseSlaStatus is "On Track" or "Met";
        var resolutionHealthy = ticket.ResolutionSlaStatus is "On Track" or "Met";

        if (responseHealthy && resolutionHealthy) return "ontrack";
        return "other";
    }

    private static SlaConfiguration DefaultConfiguration(int priorityId) => priorityId switch
    {
        1 => new SlaConfiguration { PriorityId = 1, ResponseTargetMinutes = 60, ResolutionTargetMinutes = 240 },
        2 => new SlaConfiguration { PriorityId = 2, ResponseTargetMinutes = 60, ResolutionTargetMinutes = 480 },
        3 => new SlaConfiguration { PriorityId = 3, ResponseTargetMinutes = 240, ResolutionTargetBusinessDays = 2 },
        _ => new SlaConfiguration { PriorityId = 4, ResponseTargetMinutes = 480, ResolutionTargetBusinessDays = 5 }
    };

    private static DateTime AddCalendarMinutes(DateTime start, int minutes) => start.AddMinutes(minutes);

    // One business day is represented as 8 working hours, Monday-Friday, 09:00-17:00 UTC.
    private static DateTime AddBusinessDays(DateTime start, int businessDays)
    {
        var cursor = start;
        var remaining = businessDays * 8 * 60;
        while (remaining > 0)
        {
            if (cursor.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || cursor.Hour >= 17)
            {
                cursor = cursor.Date.AddDays(1).AddHours(9);
                continue;
            }

            if (cursor.Hour < 9) cursor = cursor.Date.AddHours(9);
            var endOfDay = cursor.Date.AddHours(17);
            var available = Math.Min(remaining, (int)Math.Max(1, (endOfDay - cursor).TotalMinutes));
            cursor = cursor.AddMinutes(available);
            remaining -= available;
            if (remaining > 0 && cursor >= endOfDay) cursor = cursor.Date.AddDays(1).AddHours(9);
        }
        return cursor;
    }

    private static DateTime ShiftDueForPauses(
        DateTime createdAt,
        string currentStatus,
        DateTime due,
        List<ServiceRequestHistory> histories,
        List<string> pauseStatuses)
    {
        if (pauseStatuses.Count == 0) return due;

        var pausedDuration = TimeSpan.Zero;
        var status = currentStatus ?? "New";
        var cursor = createdAt;

        foreach (var history in histories.OrderBy(x => x.CreatedAt))
        {
            if (!string.Equals(history.Action, "Status Changed", StringComparison.OrdinalIgnoreCase))
                continue;

            var eventTime = history.CreatedAt < cursor ? cursor : history.CreatedAt;
            if (pauseStatuses.Contains(status))
                pausedDuration += eventTime - cursor;

            status = history.NewValue ?? status;
            cursor = eventTime;
        }

        if (pauseStatuses.Contains(status))
            pausedDuration += DateTime.UtcNow - cursor;

        return pausedDuration > TimeSpan.Zero ? due.Add(pausedDuration) : due;
    }

}
