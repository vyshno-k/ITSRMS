namespace ITServiceRequest.Api.Models;

public class Resolution
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public string InvestigationNotes { get; set; } = "";
    public string ResolutionNotes { get; set; } = "";
    public int? ResolvedBy { get; set; }
    public DateTime ResolvedAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public string SlaResult { get; set; } = "";
}
