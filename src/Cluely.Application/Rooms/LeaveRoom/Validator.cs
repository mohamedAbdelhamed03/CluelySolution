using FluentValidation;

namespace Cluely.Application.Rooms.LeaveRoom;

public sealed class LeaveRoomCommandValidator : AbstractValidator<LeaveRoomCommand>
{
    public LeaveRoomCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}