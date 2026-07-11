using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Results;
using Cluely.Domain.Room.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Queries.GetRoomParticipants;

public sealed class GetRoomParticipantsHandler
{
    private readonly IRoomReadModelProvider _readModelProvider;
    private readonly IValidator<GetRoomParticipantsQuery> _validator;

    public GetRoomParticipantsHandler(
        IRoomReadModelProvider readModelProvider,
        IValidator<GetRoomParticipantsQuery> validator)
    {
        _readModelProvider = readModelProvider;
        _validator = validator;
    }

    public async Task<Result<GetRoomParticipantsResult>> HandleAsync(
        GetRoomParticipantsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetRoomParticipantsResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var participants = await _readModelProvider.GetParticipantsAsync(
            RoomId.From(query.RoomId),
            cancellationToken);

        if (participants is null)
        {
            return Result.Failure<GetRoomParticipantsResult>(new BusinessError("RoomNotFound", "Room not found."));
        }

        return Result.Success(new GetRoomParticipantsResult(participants));
    }
}
