using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Room;
using Cluely.Domain.Room.Errors;
using Cluely.Domain.Room.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Rooms.CreateRoom;

public sealed class CreateRoomHandler
{
    private readonly IRoomCustody _roomCustody;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRoomCodeGenerator _roomCodeGenerator;
    private readonly IValidator<CreateRoomCommand> _validator;

    public CreateRoomHandler(
        IRoomCustody roomCustody,
        IDomainEventPublisher eventPublisher,
        IGuidGenerator guidGenerator,
        IRoomCodeGenerator roomCodeGenerator,
        IValidator<CreateRoomCommand> validator)
    {
        _roomCustody = roomCustody;
        _eventPublisher = eventPublisher;
        _guidGenerator = guidGenerator;
        _roomCodeGenerator = roomCodeGenerator;
        _validator = validator;
    }

    public async Task<Result<CreateRoomResult>> HandleAsync(CreateRoomCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<CreateRoomResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var roomId = RoomId.From(_guidGenerator.Generate());
        var roomCode = RoomCode.From(_roomCodeGenerator.Generate());
        Room room;

        try
        {
            room = Room.Create(roomId, roomCode, command.HostNickname);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CreateRoomResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<CreateRoomResult>(new UnexpectedError("UnexpectedError", "An unexpected error occurred.", ex));
        }

        await _roomCustody.SaveAsync(room, cancellationToken);
        await _eventPublisher.PublishAsync(room.GetPendingEvents(), cancellationToken);
        room.ClearPendingEvents();

        return Result.Success(new CreateRoomResult(
            roomId.Value,
            roomCode.Value,
            room.Participants.Single(p => p.IsHost).Id.Value));
    }
}