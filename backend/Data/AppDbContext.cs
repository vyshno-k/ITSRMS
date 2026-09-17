using ITServiceRequest.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ServiceCategory> Categories => Set<ServiceCategory>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestHistory> ServiceRequestHistories => Set<ServiceRequestHistory>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAssignment> TicketAssignments => Set<TicketAssignment>();
    public DbSet<Resolution> Resolutions => Set<Resolution>();
    public DbSet<SupportEngineer> SupportEngineers => Set<SupportEngineer>();
    public DbSet<SlaConfiguration> SlaConfigurations => Set<SlaConfiguration>();
    public DbSet<SlaPauseStatus> SlaPauseStatuses => Set<SlaPauseStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<ServiceCategory>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<ServiceType>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Priority>().HasIndex(x => x.Level).IsUnique();
        modelBuilder.Entity<SupportEngineer>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<SlaConfiguration>().HasIndex(x => x.PriorityId).IsUnique();
        modelBuilder.Entity<SlaPauseStatus>().HasIndex(x => x.Status).IsUnique();
        modelBuilder.Entity<ServiceRequest>().HasIndex(x => x.TicketNumber).IsUnique();

        modelBuilder.Entity<ServiceRequestHistory>()
            .HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.PerformedBy)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TicketComment>()
            .HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
