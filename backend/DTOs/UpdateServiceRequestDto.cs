namespace ITServiceRequest.Api.DTOs;

public class UpdateServiceRequestDto
{
    public int CategoryId { get; set; }

    public int ServiceTypeId { get; set; }

    public int PriorityId { get; set; }

    public string Subject { get; set; } = "";

    public string Description { get; set; } = "";
}