using FluentValidation;

namespace Cluely.Application.Queries.GetRoomParticipants;

public sealed class GetRoomParticipantsQueryValidator : AbstractValidator<GetRoomParticipantsQuery>
{
    public GetRoomParticipantsQueryValidator()
    {
        RuleFor(query => query.RoomId).NotEmpty();
        RuleFor(query => query.CorrelationId).NotEmpty();
    }
}
