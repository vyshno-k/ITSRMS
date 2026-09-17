using ITServiceManagement.API.Data;
using ITServiceManagement.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ITServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EmployeesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees([FromQuery] string? search = null)
    {
        var query = _db.Employees.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(e => e.EmployeeCode.ToLower().Contains(search) || e.FirstName.ToLower().Contains(search) || e.LastName.ToLower().Contains(search) || e.Email.ToLower().Contains(search) || e.Department.ToLower().Contains(search) || e.Designation.ToLower().Contains(search));
        }
        return Ok(await query.OrderBy(e => e.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        return employee is null ? NotFound(new { message = "Employee not found." }) : Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> AddEmployee(Employee employee)
    {
        var validation = Validate(employee);
        if (validation is not null) return BadRequest(new { message = validation });

        employee.Id = 0;
        employee.EmployeeCode = await GenerateNextEmployeeCodeAsync();
        employee.FirstName = employee.FirstName.Trim();
        employee.LastName = employee.LastName.Trim();
        employee.FullName = $"{employee.FirstName} {employee.LastName}".Trim();
        employee.Email = employee.Email.Trim();
        employee.Phone = employee.Phone.Trim();
        employee.Department = employee.Department.Trim();
        employee.Designation = employee.Designation.Trim();

        if (await _db.Employees.AnyAsync(e => e.Email.ToLower() == employee.Email.ToLower()))
            return Conflict(new { message = "Employee email already exists." });

        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Employees
                (FullName, EmployeeCode, FirstName, LastName, Email, Phone, Department, Designation, DateOfJoining, IsActive)
            VALUES
                ({employee.FullName}, {employee.EmployeeCode}, {employee.FirstName}, {employee.LastName}, {employee.Email},
                 {employee.Phone}, {employee.Department}, {employee.Designation}, {employee.DateOfJoining}, {employee.IsActive});
            """);

        employee = await _db.Employees.SingleAsync(x => x.Email == employee.Email);
        return Ok(new
        {
            success = true,
            message = "All changes saved",
            employee
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEmployee(int id, Employee input)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound(new { message = "Employee not found." });
        var validation = Validate(input);
        if (validation is not null) return BadRequest(new { message = validation });

        employee.FirstName = input.FirstName.Trim();
        employee.LastName = input.LastName.Trim();
        employee.FullName = $"{employee.FirstName} {employee.LastName}".Trim();
        employee.Email = input.Email.Trim();
        employee.Phone = input.Phone.Trim();
        employee.Department = input.Department.Trim();
        employee.Designation = input.Designation.Trim();
        employee.DateOfJoining = input.DateOfJoining;
        employee.IsActive = input.IsActive;

        if (await _db.Employees.AnyAsync(e => e.Id != id && e.Email.ToLower() == employee.Email.ToLower()))
            return Conflict(new { message = "Employee email already exists." });

        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE Employees
            SET FullName = {employee.FullName}, FirstName = {employee.FirstName}, LastName = {employee.LastName},
                Email = {employee.Email}, Phone = {employee.Phone}, Department = {employee.Department},
                Designation = {employee.Designation}, DateOfJoining = {employee.DateOfJoining}, IsActive = {employee.IsActive}
            WHERE Id = {id};
            """);
        return Ok(employee);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound(new { message = "Employee not found." });
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Employee deleted." });
    }

    private async Task<string> GenerateNextEmployeeCodeAsync()
    {
        var codes = await _db.Employees.AsNoTracking().Select(e => e.EmployeeCode).ToListAsync();
        var max = codes.Select(c => Regex.Match(c ?? "", @"^EMP(\d+)$", RegexOptions.IgnoreCase))
            .Where(m => m.Success)
            .Select(m => int.TryParse(m.Groups[1].Value, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"EMP{max + 1:000}";
    }

    private static string? Validate(Employee e)
    {
        if (string.IsNullOrWhiteSpace(e.FirstName)) return "First name is required.";
        if (e.FirstName.Trim().Length > 50) return "First name must be 50 characters or less.";
        if (string.IsNullOrWhiteSpace(e.LastName)) return "Last name is required.";
        if (e.LastName.Trim().Length > 50) return "Last name must be 50 characters or less.";
        if (string.IsNullOrWhiteSpace(e.Email)) return "Email is required.";
        if (e.Email.Trim().Length > 100) return "Email must be 100 characters or less.";
        if (!Regex.IsMatch(e.Email.Trim(), @"^\S+@\S+\.\S+$")) return "Please enter a valid email address.";
        if (string.IsNullOrWhiteSpace(e.Phone)) return "Phone is required.";
        if (e.Phone.Trim().Length > 15) return "Phone must be 15 characters or less.";
        if (string.IsNullOrWhiteSpace(e.Department)) return "Department is required.";
        if (e.Department.Trim().Length > 50) return "Department must be 50 characters or less.";
        if (string.IsNullOrWhiteSpace(e.Designation)) return "Designation is required.";
        if (e.Designation.Trim().Length > 80) return "Designation must be 80 characters or less.";
        if (e.DateOfJoining == default) return "Date of joining is required.";
        return null;
    }
}
