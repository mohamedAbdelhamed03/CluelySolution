using Cluely.Api.Contracts.Requests;
using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Gameplay.EndTurn;
using Cluely.Application.Gameplay.StartMatch;
using Cluely.Application.Gameplay.SubmitClue;
using Cluely.Application.Gameplay.SubmitGuess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// In-match gameplay commands.
/// </summary>
[Authorize]
[ApiController]
[Route("api/rooms/{roomId:guid}")]
[Produces("application/json")]
[Tags("Gameplay")]
public sealed class GameplayController : ControllerBase
{
    private readonly StartMatchHandler _startMatchHandler;
    private readonly SubmitClueHandler _submitClueHandler;
    private readonly SubmitGuessHandler _submitGuessHandler;
    private readonly EndTurnHandler _endTurnHandler;
    private readonly IParticipantContext _participantContext;

    public GameplayController(
        StartMatchHandler startMatchHandler,
        SubmitClueHandler submitClueHandler,
        SubmitGuessHandler submitGuessHandler,
        EndTurnHandler endTurnHandler,
        IParticipantContext participantContext)
    {
        _startMatchHandler = startMatchHandler;
        _submitClueHandler = submitClueHandler;
        _submitGuessHandler = submitGuessHandler;
        _endTurnHandler = endTurnHandler;
        _participantContext = participantContext;
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartMatch([FromRoute] Guid roomId, CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _startMatchHandler.HandleAsync(
            new StartMatchCommand(roomId, participantId, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("clue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitClue(
        [FromRoute] Guid roomId,
        [FromBody] SubmitClueRequest request,
        CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _submitClueHandler.HandleAsync(
            new SubmitClueCommand(roomId, participantId, request.Word, request.Count, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("guess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitGuess(
        [FromRoute] Guid roomId,
        [FromBody] SubmitGuessRequest request,
        CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _submitGuessHandler.HandleAsync(
            new SubmitGuessCommand(roomId, participantId, request.CardPosition, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("end-turn")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EndTurn([FromRoute] Guid roomId, CancellationToken cancellationToken)
    {
        var participantId = await _participantContext.ResolveRequiredParticipantIdAsync(roomId, cancellationToken);
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _endTurnHandler.HandleAsync(
            new EndTurnCommand(roomId, participantId, correlationId),
            cancellationToken);

        return result.ToActionResult(this);
    }
}
