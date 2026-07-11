using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Identity.Repositories;

public sealed class ParticipantBindingRepository : IParticipantBindingRepository
{
    private readonly IdentityDbContext _dbContext;

    public ParticipantBindingRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParticipantBinding?> GetAsync(
        Guid userId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantBindings.AsNoTracking()
            .FirstOrDefaultAsync(
                binding => binding.UserId == userId && binding.RoomId == roomId,
                cancellationToken);

        return entity is null
            ? null
            : new ParticipantBinding(entity.UserId, entity.RoomId, entity.ParticipantId, entity.CreatedAt);
    }

    public async Task CreateAsync(ParticipantBinding binding, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ParticipantBindings
            .FirstOrDefaultAsync(
                entity => entity.UserId == binding.UserId && entity.RoomId == binding.RoomId,
                cancellationToken);

        if (existing is not null)
        {
            existing.ParticipantId = binding.ParticipantId;
            existing.CreatedAt = binding.CreatedAt;
        }
        else
        {
            _dbContext.ParticipantBindings.Add(new ParticipantBindingEntity
            {
                UserId = binding.UserId,
                RoomId = binding.RoomId,
                ParticipantId = binding.ParticipantId,
                CreatedAt = binding.CreatedAt,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
