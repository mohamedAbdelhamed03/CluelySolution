using Cluely.Domain.Content;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Application.Common.Ports.Content;

public interface IDictionaryRepository
{
    Task<Dictionary?> GetAsync(DictionaryId id, CancellationToken cancellationToken = default);

    Task<Dictionary?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);

    Task AddAsync(Dictionary dictionary, Guid idempotencyKey, CancellationToken cancellationToken = default);

    Task UpdateAsync(Dictionary dictionary, CancellationToken cancellationToken = default);
}
