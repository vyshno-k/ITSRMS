namespace ITServiceRequest.Api.Models;

public class TicketComment
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }

    public int? EmployeeId { get; set; }

    public string CommentText { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}