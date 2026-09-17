using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.Models;

namespace ITServiceRequest.Api.Services;

public class HistoryService
{
    private readonly AppDbContext _db;

    public HistoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(
        int serviceRequestId,
        string action,
        string? oldValue,
        string? newValue,
        int? performedBy)
    {
        var entry = new ServiceRequestHistory
        {
            ServiceRequestId = serviceRequestId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            PerformedBy = performedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServiceRequestHistories.Add(entry);
        await _db.SaveChangesAsync();
    }
}