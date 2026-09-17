namespace ITServiceRequest.Api.Databricks;

public sealed class DatabricksProcessRequest
{
    public int? EmployeeId { get; set; }
    public int? CategoryId { get; set; }
    public int? ServiceTypeId { get; set; }
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class DatabricksPrepareResponse
{
    public string CorrelationId { get; set; } = "";
    public long JobId { get; set; }
    public Dictionary<string, string> JobParameters { get; set; } = new();
    public string Instructions { get; set; } = "";
}

public sealed class DatabricksProcessResponse
{
    public string CorrelationId { get; set; } = "";
    public long RunId { get; set; }
    public string State { get; set; } = "";
    public object? Result { get; set; }
}

public sealed class DatabricksRunStatusResponse
{
    public long RunId { get; set; }
    public string LifeCycleState { get; set; } = "";
    public string? ResultState { get; set; }
    public string? StateMessage { get; set; }
    public object? Result { get; set; }
}

internal sealed class DatabricksRunNowResponse
{
    public long run_id { get; set; }
    public long number_in_job { get; set; }
}

internal sealed class DatabricksRunGetResponse
{
    public long run_id { get; set; }
    public DatabricksRunState state { get; set; } = new();
}

internal sealed class DatabricksRunState
{
    public string life_cycle_state { get; set; } = "";
    public string? result_state { get; set; }
    public string? state_message { get; set; }
}

internal sealed class DatabricksOutputResponse
{
    public DatabricksNotebookOutput? notebook_output { get; set; }
}

internal sealed class DatabricksNotebookOutput
{
    public string? result { get; set; }
    public bool truncated { get; set; }
}
