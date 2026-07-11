using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Results;
using Cluely.Domain.Room.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Queries.GetRoomProjection;

public sealed class GetRoomProjectionHandler
{
    private readonly IRoomReadModelProvider _readModelProvider;
    private readonly IValidator<GetRoomProjectionQuery> _validator;

    public GetRoomProjectionHandler(
        IRoomReadModelProvider readModelProvider,
        IValidator<GetRoomProjectionQuery> validator)
    {
        _readModelProvider = readModelProvider;
        _validator = validator;
    }

    public async Task<Result<GetRoomProjectionResult>> HandleAsync(
        GetRoomProjectionQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetRoomProjectionResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var (projection, failureCode) = await _readModelProvider.GetRoleFilteredProjectionAsync(
            RoomId.From(query.RoomId),
            ParticipantId.From(query.ParticipantId),
            cancellationToken);

        if (failureCode is not null)
        {
            return Result.Failure<GetRoomProjectionResult>(new BusinessError(failureCode, failureCode switch
            {
                "RoomNotFound" => "Room not found.",
                "ParticipantNotFound" => "Participant not found in room.",
                _ => "Unable to load projection."
            }));
        }

        return Result.Success(new GetRoomProjectionResult(projection!));
    }
}
