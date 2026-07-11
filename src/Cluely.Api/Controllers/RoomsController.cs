using Cluely.Api.Contracts.Requests;
using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Rooms.AssignRole;
using Cluely.Application.Rooms.AssignTeam;
using Cluely.Application.Rooms.CreateRoom;
using Cluely.Application.Rooms.JoinRoom;
using Cluely.Application.Rooms.LeaveRoom;
using Cluely.Application.Rooms.SelectDictionary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Room lifecycle and lobby configuration commands.
/// </summary>
[Authorize]
[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
[Tags("Rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly CreateRoomHandler _createRoomHandler;
    private readonly JoinRoomHandler _joinRoomHandler;
    private readonly LeaveRoomHandler _leaveRoomHandler;
    private readonly AssignTeamHandler _assignTeamHandler;
    private readonly AssignRoleHandler _assignRoleHandler;
    private readonly SelectDictionaryHandler _selectDictionaryHandler;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IParticipantBindingResolver _participantBindingResolver;
    private readonly IParticipantContext _participantContext;

    public RoomsController(
        CreateRoomHandler createRoomHandler,
        JoinRoomHandler joinRoomHandler,
        LeaveRoomHandler leaveRoomHandler,
        AssignTeamHandler assignTeamHandler,
        AssignRoleHandler assignRoleHandler,
        SelectDictionaryHandler selectDictionaryHandler,
        ICurrentUserAccessor currentUserAccessor,
        IParticipantBindingResolver participantBindingResolver,
        IParticipantContext participantContext)
    {
        _createRoomHandler = createRoomHandler;
        _joinRoomHandler = joinRoomHandler;
        _leaveRoomHandler = leaveRoomHandler;
        _assignTeamHandler = assignTeamHandler;
        _assignRoleHandler = assignRoleHandler;
        _selectDictionaryHandler = selectDictionaryHandler;
        _currentUserAccessor = currentUserAccessor;
        _participantBindingResolver = participantBindingResolver;
        _participantContext = participantContext;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Contracts.Responses.CreateRoomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateRoom(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _createRoomHandler.HandleAsync(request.ToCommand(correlationId), cancellationToken);
        if (result.IsSuccess && _currentUserAccessor.UserId is Guid userId)
        {
            await _participantBindingResolver.BindAsync(
                userId,
                result.Value!.RoomId,
                result.Value.HostParticipantId,
                cancellationToken);
        }

        return result.ToActionResult(this, value => value.ToResponse(), StatusCodes.Status201Created);
    }

    [HttpPost("{roomCode}/join")]
    [ProducesResponseType(typeof(Contracts.Responses.JoinRoomResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinRoom(
        [FromRoute] string roomCode,
        [FromBody] JoinRoomRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _joinRoomHandler.HandleAsync(request.ToCommand(roomCode, correlationId), cancellationToken);
        if (result.IsSuccess && _currentUserAccessor.UserId is Guid userId)
        {
            await _participantBindingResolver.BindAsync(
                userId,
                result.Value!.RoomId,
                result.Value.ParticipantId,
                cancellationToken);
        }

        return result.ToActionResult(this, value => value.ToResponse());
    }

    [HttpPost("{roomId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LeaveRoom([FromRoute] Guid roomId, CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _leaveRoomHandler.HandleAsync(
            new LeaveRoomCommand(roomId, participantId, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPatch("{roomId:guid}/team")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignTeam(
        [FromRoute] Guid roomId,
        [FromBody] AssignTeamRequest request,
        CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _assignTeamHandler.HandleAsync(
            new AssignTeamCommand(roomId, participantId, request.Team, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPatch("{roomId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignRole(
        [FromRoute] Guid roomId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _assignRoleHandler.HandleAsync(
            new AssignRoleCommand(roomId, participantId, request.Role, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("{roomId:guid}/dictionary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SelectDictionary(
        [FromRoute] Guid roomId,
        [FromBody] SelectDictionaryRequest request,
        CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _selectDictionaryHandler.HandleAsync(
            new SelectDictionaryCommand(roomId, participantId, request.RegionCode, request.ContentVersion, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }
}
