using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Common;

public interface IContentDomainEvent : IDomainEvent
{
    DictionaryId DictionaryId { get; }
}
