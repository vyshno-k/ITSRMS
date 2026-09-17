namespace ITServiceRequest.Api.Models;

public class ServiceRequestHistory
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }

    public string Action { get; set; } = "";

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public int? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}