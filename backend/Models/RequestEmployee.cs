namespace ITServiceRequest.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
