using ITServiceRequest.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ITServiceRequest.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureResolutionColumnsAsync(db);

        var employees = new[]
        {
            new Employee { Id = 1, FullName = "Alex Johnson", Email = "alex.johnson@example.com", IsActive = true },
            new Employee { Id = 2, FullName = "Priya Sharma", Email = "priya.sharma@example.com", IsActive = true },
            new Employee { Id = 3, FullName = "Rahul Kumar", Email = "rahul.kumar@example.com", IsActive = true },
            new Employee { Id = 4, FullName = "Anita Singh", Email = "anita.singh@example.com", IsActive = true },
            new Employee { Id = 5, FullName = "David Lee", Email = "david.lee@example.com", IsActive = true }
        };
        foreach (var item in employees)
        {
            var existing = await db.Employees.FirstOrDefaultAsync(x => x.Id == item.Id);
            if (existing == null) db.Employees.Add(item);
            else { existing.FullName = item.FullName; existing.Email = item.Email; existing.IsActive = true; }
        }

        var categories = new[]
        {
            new ServiceCategory { Id = 1, Name = "Hardware", IsActive = true },
            new ServiceCategory { Id = 2, Name = "Software", IsActive = true },
            new ServiceCategory { Id = 3, Name = "Network", IsActive = true },
            new ServiceCategory { Id = 4, Name = "Access", IsActive = true },
            new ServiceCategory { Id = 5, Name = "Email", IsActive = true }
        };
        foreach (var item in categories)
        {
            var existing = await db.Categories.FirstOrDefaultAsync(x => x.Id == item.Id);
            if (existing == null) db.Categories.Add(item);
            else { existing.Name = item.Name; existing.IsActive = true; }
        }

        var serviceTypes = new[]
        {
            new ServiceType { Id = 1, Name = "Software", IsActive = true },
            new ServiceType { Id = 2, Name = "Hardware", IsActive = true },
            new ServiceType { Id = 3, Name = "Network", IsActive = true }
        };
        foreach (var item in serviceTypes)
        {
            var existing = await db.ServiceTypes.FirstOrDefaultAsync(x => x.Id == item.Id);
            if (existing == null) db.ServiceTypes.Add(item);
            else { existing.Name = item.Name; existing.IsActive = true; }
        }

        var priorities = new[]
        {
            new Priority { Id = 1, Name = "Critical", Level = 1 },
            new Priority { Id = 2, Name = "High", Level = 2 },
            new Priority { Id = 3, Name = "Medium", Level = 3 },
            new Priority { Id = 4, Name = "Low", Level = 4 }
        };
        foreach (var item in priorities)
        {
            var existing = await db.Priorities.FirstOrDefaultAsync(x => x.Id == item.Id);
            if (existing == null) db.Priorities.Add(item);
            else { existing.Name = item.Name; existing.Level = item.Level; }
        }

        var engineers = new[]
        {
            new SupportEngineer { Id = 1, FullName = "Sam Wilson", Email = "sam.wilson@example.com", IsActive = true },
            new SupportEngineer { Id = 2, FullName = "Meera Patel", Email = "meera.patel@example.com", IsActive = true },
            new SupportEngineer { Id = 3, FullName = "John Mathew", Email = "john.mathew@example.com", IsActive = true },
            new SupportEngineer { Id = 4, FullName = "Neha Rao", Email = "neha.rao@example.com", IsActive = true },
            new SupportEngineer { Id = 5, FullName = "Chris Brown", Email = "chris.brown@example.com", IsActive = true }
        };
        foreach (var item in engineers)
        {
            var existing = await db.SupportEngineers.FirstOrDefaultAsync(x => x.Id == item.Id);
            if (existing == null) db.SupportEngineers.Add(item);
            else { existing.FullName = item.FullName; existing.Email = item.Email; existing.IsActive = true; }
        }

        var slaDefaults = new[]
        {
            new SlaConfiguration { PriorityId = 1, ResponseTargetMinutes = 60, ResolutionTargetMinutes = 240, ResolutionTargetBusinessDays = 0 },
            new SlaConfiguration { PriorityId = 2, ResponseTargetMinutes = 60, ResolutionTargetMinutes = 480, ResolutionTargetBusinessDays = 0 },
            new SlaConfiguration { PriorityId = 3, ResponseTargetMinutes = 240, ResolutionTargetMinutes = 0, ResolutionTargetBusinessDays = 2 },
            new SlaConfiguration { PriorityId = 4, ResponseTargetMinutes = 480, ResolutionTargetMinutes = 0, ResolutionTargetBusinessDays = 5 }
        };
        foreach (var item in slaDefaults)
        {
            var existing = await db.SlaConfigurations.FirstOrDefaultAsync(x => x.PriorityId == item.PriorityId);
            if (existing == null) db.SlaConfigurations.Add(item);
            else
            {
                // Keep an already configured value; repair only a missing/invalid configuration.
                if (item.PriorityId == 1) existing.ResponseTargetMinutes = item.ResponseTargetMinutes;
                if (existing.ResponseTargetMinutes <= 0) existing.ResponseTargetMinutes = item.ResponseTargetMinutes;
                if (existing.ResolutionTargetMinutes < 0) existing.ResolutionTargetMinutes = item.ResolutionTargetMinutes;
                if (existing.ResolutionTargetBusinessDays < 0) existing.ResolutionTargetBusinessDays = item.ResolutionTargetBusinessDays;
            }
        }

        var workflowStatuses = new[] { "New", "Assigned", "In Progress", "Resolved", "Closed", "Reopened" };
        foreach (var status in workflowStatuses)
        {
            if (!await db.SlaPauseStatuses.AnyAsync(x => x.Status == status))
                db.SlaPauseStatuses.Add(new SlaPauseStatus { Status = status, IsActive = false });
        }

        if (!await db.ServiceRequests.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var serviceRequests = new[]
            {
                new ServiceRequest { Id = 1, TicketNumber = "SR-1001", EmployeeId = 1, CategoryId = 2, ServiceTypeId = 1, PriorityId = 2, Subject = "Laptop slow after update", Description = "Laptop is running slow after the latest update.", Status = "New", CreatedAt = now.AddDays(-8), ResponseDueAt = now.AddDays(-7), ResolutionDueAt = now.AddDays(-4) },
                new ServiceRequest { Id = 2, TicketNumber = "SR-1002", EmployeeId = 2, CategoryId = 4, ServiceTypeId = 3, PriorityId = 1, Subject = "VPN access blocked", Description = "User cannot access VPN from home office.", Status = "Assigned", CreatedAt = now.AddDays(-6), ResponseDueAt = now.AddDays(-5), ResolutionDueAt = now.AddDays(-2) },
                new ServiceRequest { Id = 3, TicketNumber = "SR-1003", EmployeeId = 3, CategoryId = 3, ServiceTypeId = 3, PriorityId = 3, Subject = "Wi-Fi instability in office", Description = "Intermittent Wi-Fi disconnections in the office.", Status = "In Progress", CreatedAt = now.AddDays(-5), ResponseDueAt = now.AddDays(-4), ResolutionDueAt = now.AddDays(-1) },
                new ServiceRequest { Id = 4, TicketNumber = "SR-1004", EmployeeId = 4, CategoryId = 1, ServiceTypeId = 2, PriorityId = 4, Subject = "Printer not responding", Description = "Network printer is not responding on the shared queue.", Status = "Resolved", CreatedAt = now.AddDays(-10), ResponseDueAt = now.AddDays(-9), ResolutionDueAt = now.AddDays(-3), ResolvedAt = now.AddDays(-2) },
                new ServiceRequest { Id = 5, TicketNumber = "SR-1005", EmployeeId = 5, CategoryId = 5, ServiceTypeId = 1, PriorityId = 2, Subject = "Outlook not syncing", Description = "Exchange sync issue on user workstation.", Status = "Closed", CreatedAt = now.AddDays(-12), ResponseDueAt = now.AddDays(-11), ResolutionDueAt = now.AddDays(-6), ResolvedAt = now.AddDays(-5), ClosedAt = now.AddDays(-4) }
            };

            db.ServiceRequests.AddRange(serviceRequests);

            db.TicketAssignments.AddRange(
                new TicketAssignment { Id = 1, ServiceRequestId = 2, EngineerId = 1, AssignedAt = now.AddDays(-5), IsCurrent = true },
                new TicketAssignment { Id = 2, ServiceRequestId = 3, EngineerId = 2, AssignedAt = now.AddDays(-4), IsCurrent = true },
                new TicketAssignment { Id = 3, ServiceRequestId = 4, EngineerId = 3, AssignedAt = now.AddDays(-3), IsCurrent = true },
                new TicketAssignment { Id = 4, ServiceRequestId = 5, EngineerId = 4, AssignedAt = now.AddDays(-6), IsCurrent = true }
            );
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureResolutionColumnsAsync(AppDbContext db)
    {
        // The supplied project already contains a SQLite database. Ensure that an
        // older copy is upgraded without requiring SQL Server or a manual migration.
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(Resolutions);";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
            }

            var additions = new Dictionary<string, string>
            {
                ["InvestigationNotes"] = "TEXT NOT NULL DEFAULT ''",
                ["ResolutionDueAt"] = "TEXT NULL",
                ["SlaResult"] = "TEXT NOT NULL DEFAULT ''"
            };

            foreach (var item in additions)
            {
                if (columns.Contains(item.Key)) continue;
                await using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE \"Resolutions\" ADD COLUMN \"{item.Key}\" {item.Value};";
                await alter.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
