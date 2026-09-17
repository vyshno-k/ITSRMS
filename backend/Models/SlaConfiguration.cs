namespace ITServiceRequest.Api.Models;

public class SlaConfiguration
{
    public int Id { get; set; }
    public int PriorityId { get; set; }
    public int ResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
    public int ResolutionTargetBusinessDays { get; set; }
}
