namespace ITServiceRequest.Api.Models;

public class TicketAssignment
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }

    public int EngineerId { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? UnassignedAt { get; set; }

    public bool IsCurrent { get; set; }
}