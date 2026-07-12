using Cluely.Application.Common.ReadModels;

namespace Cluely.Application.Content.Discovery.GetDiscoverableDictionaries;

public sealed record GetDiscoverableDictionariesResult(IReadOnlyList<DictionarySummaryReadModel> Dictionaries);
