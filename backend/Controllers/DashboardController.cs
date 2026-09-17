using ITServiceRequest.Api.Data;
using ITServiceRequest.Api.Databricks;
using ITServiceRequest.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITServiceRequest.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DatabricksService _databricks;

    public DashboardController(DatabricksService databricks)
    {
        _databricks = databricks;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var databricksStats = await _databricks.GetDashboardFromCatalogAsync();
        if (databricksStats is not null)
        {
            return Ok(databricksStats);
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            error = StatusCodes.Status503ServiceUnavailable.ToString()
            //"Dashboard statistics are unavailable. Run the Databricks job and refresh the dashboard."
        });
    }
}
