using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Queries.GetRoom;
using Cluely.Application.Queries.GetRoomParticipants;
using Cluely.Application.Queries.GetRoomProjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Read-only room projections and summaries.
/// </summary>
[Authorize]
[ApiController]
[Route("api/rooms/{roomId:guid}")]
[Produces("application/json")]
[Tags("Projections")]
public sealed class ProjectionsController : ControllerBase
{
    private readonly GetRoomHandler _getRoomHandler;
    private readonly GetRoomProjectionHandler _getRoomProjectionHandler;
    private readonly GetRoomParticipantsHandler _getRoomParticipantsHandler;
    private readonly IParticipantContext _participantContext;

    public ProjectionsController(
        GetRoomHandler getRoomHandler,
        GetRoomProjectionHandler getRoomProjectionHandler,
        GetRoomParticipantsHandler getRoomParticipantsHandler,
        IParticipantContext participantContext)
    {
        _getRoomHandler = getRoomHandler;
        _getRoomProjectionHandler = getRoomProjectionHandler;
        _getRoomParticipantsHandler = getRoomParticipantsHandler;
        _participantContext = participantContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Contracts.Responses.RoomSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoom([FromRoute] Guid roomId, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _getRoomHandler.HandleAsync(new GetRoomQuery(roomId, correlationId), cancellationToken);
        return result.ToActionResult(this, value => value.ToResponse());
    }

    [HttpGet("projection")]
    [ProducesResponseType(typeof(Contracts.Responses.RoomProjectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProjection([FromRoute] Guid roomId, CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _getRoomProjectionHandler.HandleAsync(
            new GetRoomProjectionQuery(roomId, participantId, correlationId),
            cancellationToken);

        return result.ToActionResult(this, value => value.ToResponse());
    }

    [HttpGet("participants")]
    [ProducesResponseType(typeof(Contracts.Responses.ParticipantsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParticipants([FromRoute] Guid roomId, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _getRoomParticipantsHandler.HandleAsync(
            new GetRoomParticipantsQuery(roomId, correlationId),
            cancellationToken);

        return result.ToActionResult(this, value => value.ToResponse());
    }
}
