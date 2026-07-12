using Cluely.Application.Common.ReadModels;

namespace Cluely.Application.Content.Discovery.GetMyDictionaries;

public sealed record GetMyDictionariesResult(IReadOnlyList<DictionarySummaryReadModel> Dictionaries);
