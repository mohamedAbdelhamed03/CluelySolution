using Cluely.Api.Contracts.Responses;
using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Common.ReadModels;
using Cluely.Application.Content.Discovery.GetDictionaryDetails;
using Cluely.Application.Content.Discovery.GetDictionaryVersions;
using Cluely.Application.Content.Discovery.GetDiscoverableDictionaries;
using Cluely.Application.Content.Discovery.GetMyDictionaries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Read-only discovery of dictionaries visible to the authenticated user.
/// </summary>
[Authorize]
[ApiController]
[Route("api/content")]
[Produces("application/json")]
[Tags("Content Discovery")]
public sealed class ContentDiscoveryController : ControllerBase
{
    private readonly GetMyDictionariesHandler _myHandler;
    private readonly GetDiscoverableDictionariesHandler _discoverHandler;
    private readonly GetDictionaryDetailsHandler _detailsHandler;
    private readonly GetDictionaryVersionsHandler _versionsHandler;

    public ContentDiscoveryController(
        GetMyDictionariesHandler myHandler,
        GetDiscoverableDictionariesHandler discoverHandler,
        GetDictionaryDetailsHandler detailsHandler,
        GetDictionaryVersionsHandler versionsHandler)
    {
        _myHandler = myHandler;
        _discoverHandler = discoverHandler;
        _detailsHandler = detailsHandler;
        _versionsHandler = versionsHandler;
    }

    /// <summary>Lists dictionaries owned by the authenticated user.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<ContentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var result = await _myHandler.HandleAsync(
            new GetMyDictionariesQuery(CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => value.Dictionaries.Select(ToSummary).ToList());
    }

    /// <summary>Lists public and shared-with-me dictionaries the caller can discover.</summary>
    [HttpGet("discover")]
    [ProducesResponseType(typeof(IReadOnlyList<ContentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Discover(CancellationToken cancellationToken)
    {
        var result = await _discoverHandler.HandleAsync(
            new GetDiscoverableDictionariesQuery(CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => value.Dictionaries.Select(ToSummary).ToList());
    }

    /// <summary>Returns a dictionary's details, if the caller is permitted to see it.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var result = await _detailsHandler.HandleAsync(
            new GetDictionaryDetailsQuery(id, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => ToDetails(value.Dictionary));
    }

    /// <summary>Returns the versions of a dictionary the caller is permitted to see.</summary>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<ContentVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Versions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _versionsHandler.HandleAsync(
            new GetDictionaryVersionsQuery(id, CorrelationIdAccessor.GetCorrelationId(HttpContext)),
            cancellationToken);

        return result.ToActionResult(this, value => value.Versions.Select(ToVersion).ToList());
    }

    private static ContentSummaryResponse ToSummary(DictionarySummaryReadModel model) =>
        new(model.DictionaryId, model.OwnerId, model.Title, model.Description, model.Tags, model.Language,
            model.Region, model.Visibility, model.ContentType, model.CurrentVersionId, model.CurrentVersionLabel);

    private static ContentVersionResponse ToVersion(DictionaryVersionReadModel model) =>
        new(model.VersionId, model.Label, model.PublishedAt, model.WordCount, model.LifecycleState);

    private static ContentDetailsResponse ToDetails(DictionaryDetailsReadModel model) =>
        new(model.DictionaryId, model.OwnerId, model.Title, model.Description, model.Tags, model.Language,
            model.Region, model.Visibility, model.ContentType, model.CurrentVersionId, model.CurrentVersionLabel,
            model.Versions.Select(ToVersion).ToList());
}
