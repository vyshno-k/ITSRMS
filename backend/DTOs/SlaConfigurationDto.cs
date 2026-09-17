namespace ITServiceRequest.Api.DTOs;

public class SlaConfigurationDto
{
    public int Id { get; set; }
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = "";
    public int ResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
    public int ResolutionTargetBusinessDays { get; set; }
}

public class UpdateSlaConfigurationDto
{
    public int PriorityId { get; set; }
    public int ResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
    public int ResolutionTargetBusinessDays { get; set; }
}

public class UpdateSlaPauseStatusesDto
{
    public List<string> Statuses { get; set; } = [];
}

public class SlaTicketDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = "";
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public string ResponseSlaStatus { get; set; } = "On Track";
    public string ResolutionSlaStatus { get; set; } = "On Track";
    public bool IsBreached { get; set; }
    public bool IsPaused { get; set; }
    public int? EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public string Subject { get; set; } = "";
}

public class SlaStatusDto
{
    public DateTime? ResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public string ResponseSlaStatus { get; set; } = "On Track";
    public string ResolutionSlaStatus { get; set; } = "On Track";
    public bool IsBreached { get; set; }
    public bool IsPaused { get; set; }
}
