using System.ComponentModel.DataAnnotations;

namespace ITServiceManagement.API.Models;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    [MaxLength(50)] public string FirstName { get; set; } = "";
    [MaxLength(50)] public string LastName { get; set; } = "";
    [MaxLength(100)] public string Email { get; set; } = "";
    [MaxLength(15)] public string Phone { get; set; } = "";
    [MaxLength(50)] public string Department { get; set; } = "";
    [MaxLength(80)] public string Designation { get; set; } = "";
    public DateTime DateOfJoining { get; set; }
    public bool IsActive { get; set; } = true;
}
