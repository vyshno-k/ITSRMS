namespace ITServiceRequest.Api.Models;

public class SupportEngineer
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
}