namespace ITServiceRequest.Api.Models;

public class ServiceType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
