namespace ITServiceRequest.Api.DTOs;

public class ReopenTicketDto
{
    public int ReopenedBy { get; set; }

    public string Reason { get; set; } = "";
}