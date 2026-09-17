namespace ITServiceRequest.Api.DTOs;

public class ResolveTicketDto
{
    public string InvestigationNotes { get; set; } = "";
    public string ResolutionNotes { get; set; } = "";
    public int ResolvedBy { get; set; }
    public int EngineerId { get; set; }
}
