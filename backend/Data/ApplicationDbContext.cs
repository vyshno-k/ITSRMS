using ITServiceManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ITServiceManagement.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeCode).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => x.Name).IsUnique();
    }
}
