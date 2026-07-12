using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Content.ArchiveDictionary;
using Cluely.Application.Content.BulkAddWords;
using Cluely.Application.Content.CreateDictionary;
using Cluely.Application.Content.PublishDictionary;
using Cluely.Application.Content.RemoveWord;
using Cluely.Application.Content.RenameDictionary;
using Cluely.Application.Content.ReplaceWord;
using Cluely.Application.Content.RestoreDictionary;
using Cluely.Application.Content.ValidateDraft;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Dictionary lifecycle, authoring, and publishing commands for the authenticated owner.
/// </summary>
[Authorize]
[ApiController]
[Route("api/content")]
[Produces("application/json")]
[Tags("Content")]
public sealed class ContentController : ControllerBase
{
    private readonly CreateDictionaryHandler _createHandler;
    private readonly RenameDictionaryHandler _renameHandler;
    private readonly ArchiveDictionaryHandler _archiveHandler;
    private readonly RestoreDictionaryHandler _restoreHandler;
    private readonly BulkAddWordsHandler _addWordsHandler;
    private readonly RemoveWordHandler _removeWordHandler;
    private readonly ReplaceWordHandler _replaceWordHandler;
    private readonly ValidateDraftHandler _validateHandler;
    private readonly PublishDictionaryHandler _publishHandler;

    public ContentController(
        CreateDictionaryHandler createHandler,
        RenameDictionaryHandler renameHandler,
        ArchiveDictionaryHandler archiveHandler,
        RestoreDictionaryHandler restoreHandler,
        BulkAddWordsHandler addWordsHandler,
        RemoveWordHandler removeWordHandler,
        ReplaceWordHandler replaceWordHandler,
        ValidateDraftHandler validateHandler,
        PublishDictionaryHandler publishHandler)
    {
        _createHandler = createHandler;
        _renameHandler = renameHandler;
        _archiveHandler = archiveHandler;
        _restoreHandler = restoreHandler;
        _addWordsHandler = addWordsHandler;
        _removeWordHandler = removeWordHandler;
        _replaceWordHandler = replaceWordHandler;
        _validateHandler = validateHandler;
        _publishHandler = publishHandler;
    }

    /// <summary>Creates a new dictionary owned by the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContentCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateContentRequest request, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(
            new CreateDictionaryCommand(
                request.Title, request.Description, request.Tags, request.Language, request.Region,
                request.ContentType, CorrelationIdAccessor.GetCorrelationId(HttpContext),
                IdempotencyKeyAccessor.GetIdempotencyKey(HttpContext)),
            cancellationToken);

        return result.ToActionResult(
            this,
            value => new ContentCreatedResponse(value.DictionaryId, value.Title),
            StatusCodes.Status201Created);
    }

    /// <summary>Updates a dictionary's metadata.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ContentUpdatedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContentRequest request, CancellationToken cancellationToken)
    {
        var result = await _renameHandler.HandleAsync(
            new RenameDictionaryCommand(
                id, request.Title, request.Description, request.Tags, request.Language, request.Region,
                CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => new ContentUpdatedResponse(value.DictionaryId, value.Title));
    }

    /// <summary>Archives a dictionary (soft delete); it can be restored.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _archiveHandler.HandleAsync(
            new ArchiveDictionaryCommand(id, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    /// <summary>Restores a previously archived dictionary.</summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _restoreHandler.HandleAsync(
            new RestoreDictionaryCommand(id, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    /// <summary>Adds words to the dictionary's draft.</summary>
    [HttpPost("{id:guid}/words")]
    [ProducesResponseType(typeof(WordCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddWords(Guid id, [FromBody] AddWordsRequest request, CancellationToken cancellationToken)
    {
        var result = await _addWordsHandler.HandleAsync(
            new BulkAddWordsCommand(id, request.Words, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => new WordCountResponse(value.DictionaryId, value.WordCount));
    }

    /// <summary>Removes a word from the dictionary's draft.</summary>
    [HttpDelete("{id:guid}/words/{word}")]
    [ProducesResponseType(typeof(WordCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveWord(Guid id, string word, CancellationToken cancellationToken)
    {
        var result = await _removeWordHandler.HandleAsync(
            new RemoveWordCommand(id, word, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => new WordCountResponse(value.DictionaryId, value.WordCount));
    }

    /// <summary>Replaces a word in the dictionary's draft.</summary>
    [HttpPatch("{id:guid}/words")]
    [ProducesResponseType(typeof(WordCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceWord(Guid id, [FromBody] ReplaceWordRequest request, CancellationToken cancellationToken)
    {
        var result = await _replaceWordHandler.HandleAsync(
            new ReplaceWordCommand(id, request.ExistingWord, request.NewWord, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => new WordCountResponse(value.DictionaryId, value.WordCount));
    }

    /// <summary>Validates the dictionary's draft without publishing.</summary>
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(ValidateContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Validate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _validateHandler.HandleAsync(
            new ValidateDraftCommand(id, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value =>
            new ValidateContentResponse(value.DictionaryId, value.IsValid, value.Errors, value.WordCount));
    }

    /// <summary>Publishes the dictionary's draft into a new immutable version.</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(PublishContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _publishHandler.HandleAsync(
            new PublishDictionaryCommand(
                id,
                CorrelationIdAccessor.GetCorrelationId(HttpContext),
                IdempotencyKeyAccessor.GetIdempotencyKey(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value =>
            new PublishContentResponse(value.DictionaryId, value.VersionId, value.VersionLabel, value.WordCount));
    }
}
