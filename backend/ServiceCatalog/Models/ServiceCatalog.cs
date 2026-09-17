namespace ItServiceManagement.API.ServiceCatalog.Models;

public class ServiceCatalog
{
    public int Id { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public string? ServiceOwner { get; set; }

    public string? EstimatedDeliveryTime { get; set; }

    public string DefaultPriority { get; set; } = string.Empty;

    public string SLA { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}