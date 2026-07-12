using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Content.CloneDictionary;
using Cluely.Application.Content.RevokeShare;
using Cluely.Application.Content.ShareDictionary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Sharing and cloning commands for a dictionary owner (share/revoke) and any authorized viewer (clone).
/// </summary>
[Authorize]
[ApiController]
[Route("api/content")]
[Produces("application/json")]
[Tags("Content Sharing")]
public sealed class ContentSharingController : ControllerBase
{
    private readonly ShareDictionaryHandler _shareHandler;
    private readonly RevokeShareHandler _revokeHandler;
    private readonly CloneDictionaryHandler _cloneHandler;

    public ContentSharingController(
        ShareDictionaryHandler shareHandler,
        RevokeShareHandler revokeHandler,
        CloneDictionaryHandler cloneHandler)
    {
        _shareHandler = shareHandler;
        _revokeHandler = revokeHandler;
        _cloneHandler = cloneHandler;
    }

    /// <summary>Shares a dictionary with another account.</summary>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Share(Guid id, [FromBody] ShareContentRequest request, CancellationToken cancellationToken)
    {
        var result = await _shareHandler.HandleAsync(
            new ShareDictionaryCommand(id, request.GranteeId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    /// <summary>Revokes a share grant.</summary>
    [HttpDelete("{id:guid}/share/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _revokeHandler.HandleAsync(
            new RevokeShareCommand(id, userId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    /// <summary>Clones a dictionary from one of its published versions into a new owned dictionary.</summary>
    [HttpPost("{id:guid}/clone")]
    [ProducesResponseType(typeof(CloneContentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Clone(Guid id, [FromBody] CloneContentRequest request, CancellationToken cancellationToken)
    {
        var result = await _cloneHandler.HandleAsync(
            new CloneDictionaryCommand(
                id, request.SourceVersionId, CorrelationIdAccessor.GetCorrelationId(HttpContext),
                IdempotencyKeyAccessor.GetIdempotencyKey(HttpContext)),
            cancellationToken);

        return result.ToActionResult(
            this,
            value => new CloneContentResponse(value.DictionaryId, value.SourceDictionaryId, value.SourceVersionId),
            StatusCodes.Status201Created);
    }
}
