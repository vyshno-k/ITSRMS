using ITServiceManagement.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITServiceManagement.API.Data;

public static class DatabaseSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        if (!db.Departments.Any())
        {
            db.Departments.AddRange(
                new Department { Name = "IT" },
                new Department { Name = "HR" },
                new Department { Name = "Finance" },
                new Department { Name = "Operations" });
        }

        if (!db.Employees.Any())
        {
            db.Employees.AddRange(
                new Employee
                {
                    EmployeeCode = "EMP001",
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john.smith@example.com",
                    Phone = "9876543210",
                    Department = "IT",
                    Designation = "Software Developer",
                    DateOfJoining = new DateTime(2025, 1, 10),
                    IsActive = true
                },
                new Employee
                {
                    EmployeeCode = "EMP002",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = "sarah.johnson@example.com",
                    Phone = "9876543211",
                    Department = "HR",
                    Designation = "HR Executive",
                    DateOfJoining = new DateTime(2024, 6, 15),
                    IsActive = true
                });
        }

        var hasher = new PasswordHasher<User>();
        var admin = db.Users.FirstOrDefault(user => user.Username.ToLower() == "admin");
        if (admin is null)
        {
            admin = new User { Username = "admin" };
            db.Users.Add(admin);
        }
        admin.FullName = "System Administrator";
        admin.Email = "admin@itsm.local";
        admin.Department = "IT";
        admin.Role = "Admin";
        admin.IsActive = true;
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        db.SaveChanges();
    }
}
