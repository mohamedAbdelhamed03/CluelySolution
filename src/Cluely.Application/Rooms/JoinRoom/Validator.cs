using FluentValidation;

namespace Cluely.Application.Rooms.JoinRoom;

public sealed class JoinRoomCommandValidator : AbstractValidator<JoinRoomCommand>
{
    public JoinRoomCommandValidator()
    {
        RuleFor(c => c.RoomCode).NotEmpty();
        RuleFor(c => c.Nickname).NotEmpty();
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}