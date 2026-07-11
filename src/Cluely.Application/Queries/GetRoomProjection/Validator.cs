using FluentValidation;

namespace Cluely.Application.Queries.GetRoomProjection;

public sealed class GetRoomProjectionQueryValidator : AbstractValidator<GetRoomProjectionQuery>
{
    public GetRoomProjectionQueryValidator()
    {
        RuleFor(query => query.RoomId).NotEmpty();
        RuleFor(query => query.ParticipantId).NotEmpty();
        RuleFor(query => query.CorrelationId).NotEmpty();
    }
}
