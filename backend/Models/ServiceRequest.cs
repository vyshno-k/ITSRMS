namespace ITServiceRequest.Api.Models;

public class ServiceRequest
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = "";

    public int EmployeeId { get; set; }

    public int CategoryId { get; set; }

    public int ServiceTypeId { get; set; }

    public int PriorityId { get; set; }

    public string Subject { get; set; } = "";

    public string Description { get; set; } = "";

    public string Status { get; set; } = "New";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResponseDueAt { get; set; }

    public DateTime? ResolutionDueAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }
}