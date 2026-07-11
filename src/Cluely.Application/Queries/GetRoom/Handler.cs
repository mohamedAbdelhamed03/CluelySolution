using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Results;
using Cluely.Domain.Room.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Queries.GetRoom;

public sealed class GetRoomHandler
{
    private readonly IRoomReadModelProvider _readModelProvider;
    private readonly IValidator<GetRoomQuery> _validator;

    public GetRoomHandler(
        IRoomReadModelProvider readModelProvider,
        IValidator<GetRoomQuery> validator)
    {
        _readModelProvider = readModelProvider;
        _validator = validator;
    }

    public async Task<Result<GetRoomResult>> HandleAsync(GetRoomQuery query, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetRoomResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var summary = await _readModelProvider.GetRoomSummaryAsync(
            RoomId.From(query.RoomId),
            cancellationToken);

        if (summary is null)
        {
            return Result.Failure<GetRoomResult>(new BusinessError("RoomNotFound", "Room not found."));
        }

        return Result.Success(new GetRoomResult(summary));
    }
}
