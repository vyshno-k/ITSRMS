namespace ITServiceRequest.Api.DTOs;

public class CreateServiceRequestDto
{
    public int EmployeeId { get; set; }

    public int CategoryId { get; set; }

    public int ServiceTypeId { get; set; }

    public int PriorityId { get; set; }

    public string Subject { get; set; } = "";

    public string Description { get; set; } = "";
}