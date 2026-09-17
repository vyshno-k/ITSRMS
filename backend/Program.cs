using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.Services;
using ITServiceRequest.Api.Databricks;
using ITServiceManagement.API.Data;
using ItServiceManagement.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<DatabricksOptions>(builder.Configuration.GetSection("Databricks"));
builder.Services.AddHttpClient("Databricks");
builder.Services.AddScoped<DatabricksService>();

var databaseDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "database"));
Directory.CreateDirectory(databaseDirectory);
var databaseConnection = $"Data Source={Path.Combine(databaseDirectory, "ITServiceRequest.db")};Cache=Shared;Default Timeout=30";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(databaseConnection));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(databaseConnection));
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlite(databaseConnection));

builder.Services.AddScoped<TicketNumberService>();
builder.Services.AddScoped<HistoryService>();
builder.Services.AddScoped<SlaService>();
builder.Services.AddScoped<ServiceRequestService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;

        context.Response.StatusCode = ex is InvalidOperationException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            error = ex?.Message ?? "An unexpected error occurred."
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Angular");

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(db);
    await EnsureSharedDatabaseSchemaAsync(db);

    var employeeDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DatabaseSeeder.Seed(employeeDb);

    _ = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
}

app.Run();

static async Task EnsureSharedDatabaseSchemaAsync(AppDbContext db)
{
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();
    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 30000;";
        await command.ExecuteNonQueryAsync();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Departments (
                Id INTEGER NOT NULL CONSTRAINT PK_Departments PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER NOT NULL CONSTRAINT PK_Users PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                Department TEXT NOT NULL,
                Role TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ServiceCatalogs (
                Id INTEGER NOT NULL CONSTRAINT PK_ServiceCatalogs PRIMARY KEY AUTOINCREMENT,
                ServiceName TEXT NOT NULL,
                Description TEXT NOT NULL,
                Category TEXT NOT NULL,
                RequestType TEXT NOT NULL,
                ServiceOwner TEXT NULL,
                EstimatedDeliveryTime TEXT NULL,
                DefaultPriority TEXT NOT NULL,
                SLA TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();

        var columns = new[]
        {
            "EmployeeCode TEXT NOT NULL DEFAULT ''",
            "FirstName TEXT NOT NULL DEFAULT ''",
            "LastName TEXT NOT NULL DEFAULT ''",
            "Phone TEXT NOT NULL DEFAULT ''",
            "Department TEXT NOT NULL DEFAULT ''",
            "Designation TEXT NOT NULL DEFAULT ''",
            "DateOfJoining TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'"
        };
        foreach (var column in columns)
        {
            command.CommandText = $"ALTER TABLE Employees ADD COLUMN {column};";
            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
            {
            }
        }

        command.CommandText = """
            UPDATE Employees
            SET EmployeeCode = CASE WHEN EmployeeCode = '' THEN 'EMP' || printf('%03d', Id) ELSE EmployeeCode END,
                FirstName = CASE WHEN FirstName = '' THEN FullName ELSE FirstName END,
                Department = CASE WHEN Department = '' THEN 'IT' ELSE Department END,
                Designation = CASE WHEN Designation = '' THEN 'Employee' ELSE Designation END,
                DateOfJoining = CASE WHEN DateOfJoining = '' THEN '2000-01-01 00:00:00' ELSE DateOfJoining END;
            """;
        await command.ExecuteNonQueryAsync();
    }
    finally
    {
        await connection.CloseAsync();
    }
}