namespace ITServiceRequest.Api.DTOs;

public class UpdateStatusDto
{
    public string Status { get; set; } = "";
    public int? PerformedBy { get; set; }
    public int? EngineerId { get; set; }
}
