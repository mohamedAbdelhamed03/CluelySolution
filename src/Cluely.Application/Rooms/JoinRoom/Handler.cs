using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Room;
using Cluely.Domain.Room.Errors;
using Cluely.Domain.Room.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Rooms.JoinRoom;

public sealed class JoinRoomHandler
{
    private readonly IRoomCustody _roomCustody;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IValidator<JoinRoomCommand> _validator;

    public JoinRoomHandler(
        IRoomCustody roomCustody,
        IDomainEventPublisher eventPublisher,
        IGuidGenerator guidGenerator,
        IValidator<JoinRoomCommand> validator)
    {
        _roomCustody = roomCustody;
        _eventPublisher = eventPublisher;
        _guidGenerator = guidGenerator;
        _validator = validator;
    }

    public async Task<Result<JoinRoomResult>> HandleAsync(JoinRoomCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<JoinRoomResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var roomCode = RoomCode.From(command.RoomCode);
        var room = await _roomCustody.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
            return Result.Failure<JoinRoomResult>(new BusinessError("RoomNotFound", "Room not found."));

        var participantId = ParticipantId.From(_guidGenerator.Generate());
        try
        {
            room.Join(participantId, command.Nickname);
        }
        catch (DomainException ex)
        {
            return Result.Failure<JoinRoomResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<JoinRoomResult>(new UnexpectedError("UnexpectedError", "An unexpected error occurred.", ex));
        }

        await _roomCustody.SaveAsync(room, cancellationToken);
        await _eventPublisher.PublishAsync(room.GetPendingEvents(), cancellationToken);
        room.ClearPendingEvents();

        return Result.Success(new JoinRoomResult(participantId.Value));
    }
}