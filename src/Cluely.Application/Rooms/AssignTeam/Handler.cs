using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Room;
using Cluely.Domain.Room.Errors;
using Cluely.Domain.Room.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Rooms.AssignTeam;

public sealed class AssignTeamHandler
{
    private readonly IRoomCustody _roomCustody;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IValidator<AssignTeamCommand> _validator;

    public AssignTeamHandler(
        IRoomCustody roomCustody,
        IDomainEventPublisher eventPublisher,
        IValidator<AssignTeamCommand> validator)
    {
        _roomCustody = roomCustody;
        _eventPublisher = eventPublisher;
        _validator = validator;
    }

    public async Task<Result<AssignTeamResult>> HandleAsync(AssignTeamCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<AssignTeamResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var roomId = RoomId.From(command.RoomId);
        var room = await _roomCustody.GetAsync(roomId, cancellationToken);
        if (room is null)
            return Result.Failure<AssignTeamResult>(new BusinessError("RoomNotFound", "Room not found."));

        try
        {
            room.AssignTeam(ParticipantId.From(command.ParticipantId), Team.From(command.Team));
        }
        catch (DomainException ex)
        {
            return Result.Failure<AssignTeamResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<AssignTeamResult>(new UnexpectedError("UnexpectedError", "An unexpected error occurred.", ex));
        }

        await _roomCustody.SaveAsync(room, cancellationToken);
        await _eventPublisher.PublishAsync(room.GetPendingEvents(), cancellationToken);
        room.ClearPendingEvents();

        return Result.Success(new AssignTeamResult());
    }
}