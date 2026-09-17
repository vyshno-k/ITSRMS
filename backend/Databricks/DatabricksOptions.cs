namespace ITServiceRequest.Api.Databricks;

public sealed class DatabricksOptions
{
    public string Host { get; set; } = "";
    public string Token { get; set; } = "";
    public long JobId { get; set; }
    public string WarehouseId { get; set; } = "";
    public string VolumePath { get; set; } = "/Volumes/srms_catalog/srms/integration";
    public int PollIntervalSeconds { get; set; } = 3;
    public int PollTimeoutSeconds { get; set; } = 600;
}
