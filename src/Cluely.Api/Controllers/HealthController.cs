using Cluely.Api.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Service health endpoints.
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
[Tags("Health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns API health status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new HealthResponse("Healthy", DateTime.UtcNow));
    }
}
