namespace ITServiceRequest.Api.Models;

public class SlaPauseStatus
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
