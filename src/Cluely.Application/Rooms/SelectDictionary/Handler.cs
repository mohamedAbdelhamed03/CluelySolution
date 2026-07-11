using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Room;
using Cluely.Domain.Room.Errors;
using Cluely.Domain.Room.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Rooms.SelectDictionary;

public sealed class SelectDictionaryHandler
{
    private readonly IRoomCustody _roomCustody;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IValidator<SelectDictionaryCommand> _validator;

    public SelectDictionaryHandler(
        IRoomCustody roomCustody,
        IDomainEventPublisher eventPublisher,
        IValidator<SelectDictionaryCommand> validator)
    {
        _roomCustody = roomCustody;
        _eventPublisher = eventPublisher;
        _validator = validator;
    }

    public async Task<Result<SelectDictionaryResult>> HandleAsync(SelectDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<SelectDictionaryResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var roomId = RoomId.From(command.RoomId);
        var room = await _roomCustody.GetAsync(roomId, cancellationToken);
        if (room is null)
            return Result.Failure<SelectDictionaryResult>(new BusinessError("RoomNotFound", "Room not found."));

        try
        {
            room.SelectDictionary(DictionaryReference.Create(RegionCode.From(command.RegionCode), ContentVersion.From(command.ContentVersion)));
        }
        catch (DomainException ex)
        {
            return Result.Failure<SelectDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<SelectDictionaryResult>(new UnexpectedError("UnexpectedError", "An unexpected error occurred.", ex));
        }

        await _roomCustody.SaveAsync(room, cancellationToken);
        await _eventPublisher.PublishAsync(room.GetPendingEvents(), cancellationToken);
        room.ClearPendingEvents();

        return Result.Success(new SelectDictionaryResult());
    }
}