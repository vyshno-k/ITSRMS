using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ITServiceManagement.API.Data;
using ITServiceRequest.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ITServiceRequest.Api.Databricks;

public sealed class DatabricksService
{
    private readonly AppDbContext _appDb;
    private readonly ApplicationDbContext _employeeDb;
    private readonly DatabricksOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatabricksService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public DatabricksService(
        AppDbContext appDb,
        ApplicationDbContext employeeDb,
        IOptions<DatabricksOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<DatabricksService> logger)
    {
        _appDb = appDb;
        _employeeDb = employeeDb;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ============================================================
    // CONFIGURATION
    // ============================================================

    public bool IsConfigured(out string message)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            message = "Databricks:Host is not configured.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            message = "Databricks:Token is not configured.";
            return false;
        }

        if (_options.JobId <= 0)
        {
            message = "Databricks:JobId is not configured.";
            return false;
        }

        if (!IsVolumePathValid(_options.VolumePath))
        {
            message =
                "Databricks:VolumePath must be in the format " +
                "/Volumes/<catalog>/<schema>/<volume>.";

            return false;
        }

        message = "Databricks integration is configured.";
        return true;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured(out var message))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static bool IsVolumePathValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var parts = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        return parts.Length >= 4 &&
               string.Equals(
                   parts[0],
                   "Volumes",
                   StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // PREPARE
    // ============================================================

    public async Task<DatabricksPrepareResponse> PrepareAsync(
        DatabricksProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var correlationId =
            Guid.NewGuid().ToString("N");

        var runRoot =
            $"{_options.VolumePath.TrimEnd('/')}/runs/{correlationId}";

        _logger.LogInformation(
            "Preparing Databricks processing. CorrelationId={CorrelationId}, RunRoot={RunRoot}",
            correlationId,
            runRoot);

        // --------------------------------------------------------
        // EMPLOYEES
        // --------------------------------------------------------

        await UploadJsonAsync(
            $"{runRoot}/employees.json",
            await BuildEmployeesSnapshotAsync(cancellationToken),
            cancellationToken);

        // --------------------------------------------------------
        // DEPARTMENTS
        // --------------------------------------------------------

        await UploadJsonAsync(
            $"{runRoot}/departments.json",
            await BuildDepartmentsSnapshotAsync(cancellationToken),
            cancellationToken);

        // --------------------------------------------------------
        // CATEGORIES
        // --------------------------------------------------------

        await UploadJsonAsync(
            $"{runRoot}/service_categories.json",
            await BuildCategoriesSnapshotAsync(cancellationToken),
            cancellationToken);

        // --------------------------------------------------------
        // SERVICE TYPES
        // --------------------------------------------------------

        await UploadJsonAsync(
            $"{runRoot}/service_types.json",
            await BuildServiceTypesSnapshotAsync(cancellationToken),
            cancellationToken);

        // --------------------------------------------------------
        // PRIORITIES
        // --------------------------------------------------------

        await UploadJsonAsync(
            $"{runRoot}/priorities.json",
            await BuildPrioritiesSnapshotAsync(cancellationToken),
            cancellationToken);

        // --------------------------------------------------------
        // SLA CONFIGURATIONS
        // --------------------------------------------------------

        await UploadJsonAsync(
            $"{runRoot}/sla_configurations.json",
            await BuildSlaSnapshotAsync(cancellationToken),
            cancellationToken);

        // --------------------------------------------------------
        // CURRENT REQUEST
        // --------------------------------------------------------

        await UploadJsonAsync(
            $"{runRoot}/request.json",
            request,
            cancellationToken);

        var resultPath =
            $"{runRoot}/result.json";

        var parameters =
            BuildJobParameters(
                runRoot,
                resultPath);

        _logger.LogInformation(
            "Databricks preparation completed. CorrelationId={CorrelationId}",
            correlationId);

        return new DatabricksPrepareResponse
        {
            CorrelationId = correlationId,
            JobId = _options.JobId,
            JobParameters = parameters,
            Instructions =
                "JSON snapshots were uploaded to the configured " +
                "Unity Catalog volume."
        };
    }

    // ============================================================
    // PROCESS
    // ============================================================

    public async Task<object?> GetDashboardFromCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning(
                "Databricks dashboard lookup skipped because Host or Token is not configured.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.WarehouseId))
        {
            _logger.LogWarning(
                "Databricks dashboard lookup skipped because WarehouseId is not configured.");
            return null;
        }

        try
        {
            var sql = "SELECT * FROM srms_catalog.srms.dashboard_stats";
            var payload = new
            {
                warehouse_id = "7b5772fc8c01896d",
                statement = sql,
                disposition = "INLINE",
                format = "JSON_ARRAY",
                wait_timeout = "30s"
            };

            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.Token);
            using var content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(
                "/api/2.0/sql/statements",
                content,
                cancellationToken);

           var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Databricks dashboard_stats query failed. StatusCode={StatusCode}, Body={Body}",
                    (int)response.StatusCode,
                    body);
                return null;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("statement_id", out var statementId))
            {
                body = await WaitForDashboardStatementAsync(
                    client,
                    statementId.GetString() ?? string.Empty,
                    cancellationToken);
            }

            using var resultDocument = JsonDocument.Parse(body);
            if (!resultDocument.RootElement.TryGetProperty("manifest", out var manifest) ||
                !manifest.TryGetProperty("schema", out var schema) ||
                !schema.TryGetProperty("columns", out var columns) ||
                !resultDocument.RootElement.TryGetProperty("result", out var result) ||
                !result.TryGetProperty("data_array", out var data) ||
                columns.ValueKind != JsonValueKind.Array ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
            {
                return null;
            }

            if (data[0].ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var rows = new List<Dictionary<string, object?>>();
            foreach (var row in data.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < row.GetArrayLength() && index < columns.GetArrayLength(); index++)
                {
                    if (columns[index].TryGetProperty("name", out var name))
                    {
                        var columnName = NormalizeDashboardColumnName(name.GetString());
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            values[columnName] = ConvertDatabricksValue(row[index]);
                        }
                    }
                }

                rows.Add(values);
            }

            var categoryRows = rows
                .Where(row => string.Equals(GetText(row, "stat_name"), "tickets_by_category", StringComparison.OrdinalIgnoreCase))
                .Select(row => new { name = GetText(row, "stat_category") ?? "Unknown", count = GetNumber(row, "stat_value") })
                .ToArray();
            var priorityRows = rows
                .Where(row => string.Equals(GetText(row, "stat_name"), "tickets_by_priority", StringComparison.OrdinalIgnoreCase))
                .Select(row => new { name = GetText(row, "stat_category") ?? "Unknown", count = GetNumber(row, "stat_value") })
                .ToArray();
            var engineerRows = rows
                .Where(row => string.Equals(GetText(row, "stat_name"), "engineer_workload", StringComparison.OrdinalIgnoreCase))
                .Select((row, index) => new { id = index + 1, name = GetText(row, "stat_category") ?? "Unknown", isActive = true, count = GetNumber(row, "stat_value") })
                .ToArray();

            return new
            {
                total = GetNumber(rows, "total_requests"),
                open = GetNumber(rows, "open_tickets"),
                inProgress = GetNumber(rows, "inprogress_tickets"),
                resolved = GetNumber(rows, "resolved_tickets"),
                closed = GetNumber(rows, "closed_tickets"),
                slaBreached = GetNumber(rows, "sla_breached_tickets"),
                byPriority = priorityRows,
                byCategory = categoryRows,
                engineerWorkload = engineerRows
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Databricks dashboard_stats table lookup failed.");
            return null;
        }
    }

    private async Task<string> WaitForDashboardStatementAsync(
        HttpClient client,
        string statementId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(statementId))
        {
            throw new InvalidOperationException("Databricks returned an empty dashboard statement ID.");
        }

        var timeout = TimeSpan.FromSeconds(Math.Max(15, _options.PollTimeoutSeconds));
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < timeout)
        {
            using var response = await client.GetAsync(
                $"/api/2.0/sql/statements/{Uri.EscapeDataString(statementId)}",
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Databricks dashboard statement lookup failed: {body}");
            }

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("status", out var status) &&
                status.TryGetProperty("state", out var state))
            {
                var stateText = state.GetString();
                if (string.Equals(stateText, "SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                {
                    return body;
                }

                if (string.Equals(stateText, "FAILED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stateText, "CANCELED", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Databricks dashboard statement {stateText}: {body}");
                }
            }

            await Task.Delay(
                TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)),
                cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for the Databricks dashboard_stats query.");
    }

    private static string? GetText(IReadOnlyDictionary<string, object?> row, string key)
    {
        return row.TryGetValue(NormalizeDashboardColumnName(key), out var value)
            ? value?.ToString()?.Trim()
            : null;
    }

    private static int GetNumber(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(NormalizeDashboardColumnName(key), out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            int number => number,
            long number => (int)number,
            decimal number => (int)number,
            double number => (int)number,
            float number => (int)number,
            _ when int.TryParse(value.ToString(), out var number) => number,
            _ => 0
        };
    }

    private static string NormalizeDashboardColumnName(string? name)
    {
        return (name ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static int GetNumber(IEnumerable<IReadOnlyDictionary<string, object?>> rows, string statName)
    {
        var row = rows.FirstOrDefault(candidate =>
            string.Equals(GetText(candidate, "stat_name"), statName, StringComparison.OrdinalIgnoreCase));
        return row is null ? 0 : GetNumber(row, "stat_value");
    }

    public async Task<DatabricksProcessResponse> ProcessAsync(
        DatabricksProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogInformation(
            "Starting Databricks processing.");

        var prepared =
            await PrepareAsync(
                request,
                cancellationToken);

        _logger.LogInformation(
            "Starting Databricks job. JobId={JobId}, CorrelationId={CorrelationId}",
            prepared.JobId,
            prepared.CorrelationId);

        var runId =
            await RunJobAsync(
                prepared.JobParameters,
                cancellationToken);

        _logger.LogInformation(
            "Databricks job started. JobId={JobId}, RunId={RunId}",
            prepared.JobId,
            runId);

        var status =
            await WaitForCompletionAsync(
                runId,
                cancellationToken);

        object? result = null;

        if (string.Equals(
                status.ResultState,
                "SUCCESS",
                StringComparison.OrdinalIgnoreCase))
        {
            result =
                await GetNotebookResultAsync(
                    runId,
                    cancellationToken);
        }

        return new DatabricksProcessResponse
        {
            CorrelationId = prepared.CorrelationId,
            RunId = runId,
            State =
                status.ResultState ??
                status.LifeCycleState,
            Result = result
        };
    }

    // ============================================================
    // PUBLIC RUN STATUS
    // ============================================================

    public async Task<DatabricksRunStatusResponse> GetRunStatusAsync(
        long runId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (runId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runId),
                "Run ID must be greater than zero.");
        }

        _logger.LogInformation(
            "Getting Databricks run status. RunId={RunId}",
            runId);

        var run =
            await GetRunAsync(
                runId,
                cancellationToken);

        return new DatabricksRunStatusResponse
        {
            RunId = runId,

            LifeCycleState =
                run.state.life_cycle_state,

            ResultState =
                run.state.result_state,

            StateMessage =
                run.state.state_message
        };
    }

    // ============================================================
    // JOB PARAMETERS
    // ============================================================

    private Dictionary<string, string> BuildJobParameters(
        string runRoot,
        string resultPath)
    {
        return new Dictionary<string, string>
        {
            ["employees_path"] =
                $"{runRoot}/employees.json",

            ["departments_path"] =
                $"{runRoot}/departments.json",

            ["service_categories_path"] =
                $"{runRoot}/service_categories.json",

            ["service_types_path"] =
                $"{runRoot}/service_types.json",

            ["priorities_path"] =
                $"{runRoot}/priorities.json",

            ["sla_configurations_path"] =
                $"{runRoot}/sla_configurations.json",

            ["request_path"] =
                $"{runRoot}/request.json",

            ["result_path"] =
                resultPath
        };
    }

    // ============================================================
    // RUN DATABRICKS JOB
    // ============================================================

    private async Task<long> RunJobAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();

        var payload = new
        {
            job_id = _options.JobId,

            job_parameters = parameters,

            idempotency_token =
                Guid.NewGuid()
                    .ToString("N")[..32]
        };

        var json =
            JsonSerializer.Serialize(
                payload,
                JsonOptions);

        _logger.LogInformation(
            "Databricks run-now request. JobId={JobId}, Payload={Payload}",
            _options.JobId,
            json);

        using var content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

        using var response =
            await client.PostAsync(
                "/api/2.2/jobs/run-now",
                content,
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Databricks run-now failed. " +
                "StatusCode={StatusCode}, " +
                "Reason={Reason}, " +
                "Body={Body}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                body);

            throw new InvalidOperationException(
                $"Databricks run-now failed " +
                $"({(int)response.StatusCode} {response.StatusCode}). " +
                body);
        }

        _logger.LogInformation(
            "Databricks run-now response: {Body}",
            body);

        var run =
            JsonSerializer.Deserialize<DatabricksRunNowResponse>(
                body,
                JsonOptions);

        if (run == null || run.run_id <= 0)
        {
            throw new InvalidOperationException(
                "Databricks did not return a valid run ID. " +
                $"Response: {body}");
        }

        return run.run_id;
    }

    // ============================================================
    // WAIT FOR JOB
    // ============================================================

    private async Task<DatabricksRunStatusResponse>
        WaitForCompletionAsync(
            long runId,
            CancellationToken cancellationToken)
    {
        var timeoutSeconds =
            Math.Max(
                30,
                _options.PollTimeoutSeconds);

        var pollSeconds =
            Math.Max(
                1,
                _options.PollIntervalSeconds);

        var deadline =
            DateTime.UtcNow.AddSeconds(
                timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            var run =
                await GetRunAsync(
                    runId,
                    cancellationToken);

            var lifeCycleState =
                run.state.life_cycle_state;

            var resultState =
                run.state.result_state;

            _logger.LogInformation(
                "Databricks run status. " +
                "RunId={RunId}, " +
                "LifeCycleState={LifeCycleState}, " +
                "ResultState={ResultState}",
                runId,
                lifeCycleState,
                resultState);

            if (lifeCycleState is
                "TERMINATED" or
                "SKIPPED" or
                "INTERNAL_ERROR")
            {
                if (!string.Equals(
                        resultState,
                        "SUCCESS",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Databricks job run {runId} " +
                        $"finished with state " +
                        $"{lifeCycleState}/{resultState}: " +
                        $"{run.state.state_message}");
                }

                return new DatabricksRunStatusResponse
                {
                    RunId = runId,

                    LifeCycleState =
                        lifeCycleState,

                    ResultState =
                        resultState,

                    StateMessage =
                        run.state.state_message
                };
            }

            await Task.Delay(
                TimeSpan.FromSeconds(pollSeconds),
                cancellationToken);
        }

        throw new TimeoutException(
            $"Databricks job run {runId} did not finish " +
            $"within {timeoutSeconds} seconds.");
    }

    // ============================================================
    // GET RUN
    // ============================================================

    private async Task<DatabricksRunGetResponse>
        GetRunAsync(
            long runId,
            CancellationToken cancellationToken)
    {
        var client = CreateClient();

        using var response =
            await client.GetAsync(
                $"/api/2.2/jobs/runs/get?run_id={runId}",
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Databricks run status failed. " +
                "RunId={RunId}, Status={Status}, Body={Body}",
                runId,
                response.StatusCode,
                body);

            throw new InvalidOperationException(
                $"Databricks run status failed " +
                $"({(int)response.StatusCode} " +
                $"{response.StatusCode}). {body}");
        }

        return JsonSerializer.Deserialize<DatabricksRunGetResponse>(
                   body,
                   JsonOptions)
               ?? throw new InvalidOperationException(
                   "Databricks returned an empty run status response.");
    }

    private static object? ConvertDatabricksValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt32(out var intValue) ? intValue : value.TryGetInt64(out var longValue) ? longValue : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => value.Deserialize<object?>(),
            JsonValueKind.Object => value.Deserialize<object?>(),
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }

    private static int? TryGetInt(IReadOnlyDictionary<string, object?> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && value is not null)
            {
                if (value is int i)
                {
                    return i;
                }

                if (value is long l)
                {
                    return (int)l;
                }

                if (value is string s && int.TryParse(s, out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    private static object? TryGetCollection(IReadOnlyDictionary<string, object?> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!values.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            return ParseCollectionValue(value);
        }

        return null;
    }

    private static object? ParseCollectionValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Array or JsonValueKind.Object => element.Deserialize<object?>(),
                _ => element.ToString()
            };
        }

        if (value is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<object>();
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                {
                    return doc.RootElement.Deserialize<object?>();
                }
            }
            catch (JsonException)
            {
                return Array.Empty<object>();
            }

            return Array.Empty<object>();
        }

        return value;
    }

    // ============================================================
    // GET NOTEBOOK OUTPUT
    // ============================================================

    private async Task<object?>
        GetNotebookResultAsync(
            long runId,
            CancellationToken cancellationToken)
    {
        var client = CreateClient();

        using var response =
            await client.GetAsync(
                $"/api/2.2/jobs/runs/get-output?run_id={runId}",
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Databricks run output failed. " +
                "RunId={RunId}, Status={Status}, Body={Body}",
                runId,
                response.StatusCode,
                body);

            throw new InvalidOperationException(
                $"Databricks run output failed " +
                $"({(int)response.StatusCode} " +
                $"{response.StatusCode}). {body}");
        }

        var output =
            JsonSerializer.Deserialize<DatabricksOutputResponse>(
                body,
                JsonOptions);

        var resultText =
            output?.notebook_output?.result;

        if (string.IsNullOrWhiteSpace(resultText))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(
                resultText,
                JsonOptions);
        }
        catch (JsonException)
        {
            return resultText;
        }
    }

    // ============================================================
    // UPLOAD JSON TO UNITY CATALOG VOLUME
    // ============================================================

    private async Task UploadJsonAsync(
        string volumeFilePath,
        object value,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();

        var json =
            JsonSerializer.Serialize(
                value,
                JsonOptions);

        using var content =
            new ByteArrayContent(
                Encoding.UTF8.GetBytes(json));

        content.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/json");

        var encodedPath =
            Uri.EscapeDataString(volumeFilePath)
                .Replace(
                    "%2F",
                    "/",
                    StringComparison.OrdinalIgnoreCase);

        var url =
            $"/api/2.0/fs/files{encodedPath}?overwrite=true";

        _logger.LogInformation(
            "Uploading Databricks JSON file. Path={Path}",
            volumeFilePath);

        using var response =
            await client.PutAsync(
                url,
                content,
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Databricks file upload failed. " +
                "Path={Path}, " +
                "StatusCode={StatusCode}, " +
                "Reason={Reason}, " +
                "Body={Body}",
                volumeFilePath,
                (int)response.StatusCode,
                response.ReasonPhrase,
                body);

            throw new InvalidOperationException(
                $"Databricks file upload failed " +
                $"({(int)response.StatusCode} " +
                $"{response.StatusCode}) " +
                $"for {volumeFilePath}. {body}");
        }

        _logger.LogInformation(
            "Databricks file uploaded successfully. Path={Path}",
            volumeFilePath);
    }

    // ============================================================
    // HTTP CLIENT
    // ============================================================

    private HttpClient CreateClient()
    {
        var client =
            _httpClientFactory.CreateClient(
                "Databricks");

        client.BaseAddress =
            new Uri(
                _options.Host.TrimEnd('/') + "/");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.Token);

        if (!client.DefaultRequestHeaders.Accept.Any())
        {
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));
        }

        return client;
    }

    // ============================================================
    // EMPLOYEES
    // ============================================================

    private async Task<object>
        BuildEmployeesSnapshotAsync(
            CancellationToken cancellationToken)
    {
        var requestEmployees =
            await _appDb.Employees
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        var managedEmployees =
            await _employeeDb.Employees
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        var result =
            new List<object>();

        result.AddRange(
            requestEmployees.Select(
                x => new
                {
                    id = x.Id,

                    employeeCode =
                        $"REQ-{x.Id:0000}",

                    firstName =
                        x.FullName,

                    lastName = "",

                    fullName =
                        x.FullName,

                    email =
                        x.Email,

                    phone = "",

                    department = "",

                    designation = "",

                    dateOfJoining =
                        (DateTime?)null,

                    isActive =
                        x.IsActive,

                    source =
                        "service_requests"
                }));

        result.AddRange(
            managedEmployees.Select(
                x => new
                {
                    id = x.Id,

                    employeeCode =
                        x.EmployeeCode,

                    firstName =
                        x.FirstName,

                    lastName =
                        x.LastName,

                    fullName =
                        $"{x.FirstName} {x.LastName}".Trim(),

                    email =
                        x.Email,

                    phone =
                        x.Phone,

                    department =
                        x.Department,

                    designation =
                        x.Designation,

                    dateOfJoining =
                        (DateTime?)x.DateOfJoining,

                    isActive =
                        x.IsActive,

                    source =
                        "employee_management"
                }));

        return result;
    }

    // ============================================================
    // DEPARTMENTS
    // ============================================================

    private async Task<object>
        BuildDepartmentsSnapshotAsync(
            CancellationToken cancellationToken)
    {
        return await _employeeDb.Departments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(
                x => new
                {
                    id = x.Id,
                    name = x.Name
                })
            .ToListAsync(cancellationToken);
    }

    // ============================================================
    // CATEGORIES
    // ============================================================

    private async Task<object>
        BuildCategoriesSnapshotAsync(
            CancellationToken cancellationToken)
    {
        return await _appDb.Categories
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(
                x => new
                {
                    id = x.Id,
                    name = x.Name,
                    isActive = x.IsActive
                })
            .ToListAsync(cancellationToken);
    }

    // ============================================================
    // SERVICE TYPES
    // ============================================================

    private async Task<object>
        BuildServiceTypesSnapshotAsync(
            CancellationToken cancellationToken)
    {
        return await _appDb.ServiceTypes
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(
                x => new
                {
                    id = x.Id,
                    name = x.Name,
                    isActive = x.IsActive
                })
            .ToListAsync(cancellationToken);
    }

    // ============================================================
    // PRIORITIES
    // ============================================================

    private async Task<object>
        BuildPrioritiesSnapshotAsync(
            CancellationToken cancellationToken)
    {
        return await _appDb.Priorities
            .AsNoTracking()
            .OrderBy(x => x.Level)
            .Select(
                x => new
                {
                    id = x.Id,
                    name = x.Name,
                    level = x.Level
                })
            .ToListAsync(cancellationToken);
    }

    // ============================================================
    // SLA CONFIGURATION
    // ============================================================

    private async Task<object>
        BuildSlaSnapshotAsync(
            CancellationToken cancellationToken)
    {
        return await _appDb.SlaConfigurations
            .AsNoTracking()
            .OrderBy(x => x.PriorityId)
            .Select(
                x => new
                {
                    priorityId =
                        x.PriorityId,

                    responseTargetMinutes =
                        x.ResponseTargetMinutes,

                    resolutionTargetMinutes =
                        x.ResolutionTargetMinutes,

                    resolutionTargetBusinessDays =
                        x.ResolutionTargetBusinessDays
                })
            .ToListAsync(cancellationToken);
    }
}