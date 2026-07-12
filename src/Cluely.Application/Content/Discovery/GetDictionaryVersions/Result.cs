using Cluely.Application.Common.ReadModels;

namespace Cluely.Application.Content.Discovery.GetDictionaryVersions;

public sealed record GetDictionaryVersionsResult(IReadOnlyList<DictionaryVersionReadModel> Versions);
