namespace ITServiceRequest.Api.DTOs;

public class ServiceRequestViewDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = "";
    public int EmployeeId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = "";
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public DateTime? EffectiveResponseDueAt { get; set; }
    public DateTime? EffectiveResolutionDueAt { get; set; }
    public string ResponseSlaStatus { get; set; } = "On Track";
    public string ResolutionSlaStatus { get; set; } = "On Track";
    public bool IsSlaBreached { get; set; }
    public bool IsSlaPaused { get; set; }
    public ResolutionViewDto? LatestResolution { get; set; }
}
