using Cluely.Api.Contracts.Requests;
using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Content.ApproveReview;
using Cluely.Application.Content.BlockVersion;
using Cluely.Application.Content.RejectReview;
using Cluely.Application.Content.RetireVersion;
using Cluely.Application.Content.SubmitForReview;
using Cluely.Application.Content.UnblockVersion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Review and moderation lifecycle commands for dictionary versions. Submit and retire are owner
/// actions; approve/reject/block/unblock require the moderator role (enforced in the Application layer).
/// </summary>
[Authorize]
[ApiController]
[Route("api/content")]
[Produces("application/json")]
[Tags("Content Moderation")]
public sealed class ContentModerationController : ControllerBase
{
    private readonly SubmitForReviewHandler _submitHandler;
    private readonly ApproveReviewHandler _approveHandler;
    private readonly RejectReviewHandler _rejectHandler;
    private readonly BlockVersionHandler _blockHandler;
    private readonly UnblockVersionHandler _unblockHandler;
    private readonly RetireVersionHandler _retireHandler;

    public ContentModerationController(
        SubmitForReviewHandler submitHandler,
        ApproveReviewHandler approveHandler,
        RejectReviewHandler rejectHandler,
        BlockVersionHandler blockHandler,
        UnblockVersionHandler unblockHandler,
        RetireVersionHandler retireHandler)
    {
        _submitHandler = submitHandler;
        _approveHandler = approveHandler;
        _rejectHandler = rejectHandler;
        _blockHandler = blockHandler;
        _unblockHandler = unblockHandler;
        _retireHandler = retireHandler;
    }

    /// <summary>Submits a published version for moderation review (owner).</summary>
    [HttpPost("{id:guid}/submit-review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> SubmitReview(Guid id, [FromBody] VersionActionRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _submitHandler.HandleAsync(
            new SubmitForReviewCommand(id, request.VersionId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken));
    }

    /// <summary>Approves a version under review, making it discoverable (moderator).</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Approve(Guid id, [FromBody] VersionActionRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _approveHandler.HandleAsync(
            new ApproveReviewCommand(id, request.VersionId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken));
    }

    /// <summary>Rejects a version under review (moderator).</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Reject(Guid id, [FromBody] VersionActionRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _rejectHandler.HandleAsync(
            new RejectReviewCommand(id, request.VersionId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken));
    }

    /// <summary>Blocks a version from discovery/selection (moderator).</summary>
    [HttpPost("{id:guid}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Block(Guid id, [FromBody] VersionActionRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _blockHandler.HandleAsync(
            new BlockVersionCommand(id, request.VersionId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken));
    }

    /// <summary>Unblocks a version, returning it to review (moderator).</summary>
    [HttpPost("{id:guid}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Unblock(Guid id, [FromBody] VersionActionRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _unblockHandler.HandleAsync(
            new UnblockVersionCommand(id, request.VersionId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken));
    }

    /// <summary>Retires a version from all new use (moderator).</summary>
    [HttpPost("{id:guid}/retire")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Retire(Guid id, [FromBody] VersionActionRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _retireHandler.HandleAsync(
            new RetireVersionCommand(id, request.VersionId, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken));
    }

    private async Task<IActionResult> ExecuteAsync<TResult>(Func<Task<Application.Common.Results.Result<TResult>>> action)
    {
        var result = await action();
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}
