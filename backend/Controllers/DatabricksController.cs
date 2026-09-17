using ITServiceRequest.Api.Databricks;
using Microsoft.AspNetCore.Mvc;

namespace ITServiceRequest.Api.Controllers;

[ApiController]
[Route("api/databricks")]
public sealed class DatabricksController : ControllerBase
{
    private readonly DatabricksService _service;

    public DatabricksController(DatabricksService service)
    {
        _service = service;
    }

    // ============================================================
    // DATABRICKS CONFIGURATION STATUS
    // GET /api/databricks/status
    // ============================================================

    [HttpGet("status")]
    public IActionResult Status()
    {
        var configured =
            _service.IsConfigured(out var message);

        return Ok(new
        {
            configured,
            message
        });
    }

    // ============================================================
    // PREPARE
    // POST /api/databricks/prepare
    // ============================================================

    [HttpPost("prepare")]
    public async Task<ActionResult<DatabricksPrepareResponse>> Prepare(
        [FromBody] DatabricksProcessRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new DatabricksProcessRequest();

        var result =
            await _service.PrepareAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    // ============================================================
    // PROCESS
    // POST /api/databricks/process
    // ============================================================

    [HttpPost("process")]
    public async Task<ActionResult<DatabricksProcessResponse>> Process(
        [FromBody] DatabricksProcessRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new DatabricksProcessRequest();

        var result =
            await _service.ProcessAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    // ============================================================
    // GET RUN STATUS
    // GET /api/databricks/runs/{runId}
    // ============================================================

    [HttpGet("runs/{runId:long}")]
    public async Task<ActionResult<DatabricksRunStatusResponse>> GetRun(
        long runId,
        CancellationToken cancellationToken)
    {
        var result =
            await _service.GetRunStatusAsync(
                runId,
                cancellationToken);

        return Ok(result);
    }
}