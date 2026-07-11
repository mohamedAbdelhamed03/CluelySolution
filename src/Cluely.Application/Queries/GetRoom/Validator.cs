using FluentValidation;

namespace Cluely.Application.Queries.GetRoom;

public sealed class GetRoomQueryValidator : AbstractValidator<GetRoomQuery>
{
    public GetRoomQueryValidator()
    {
        RuleFor(query => query.RoomId).NotEmpty();
        RuleFor(query => query.CorrelationId).NotEmpty();
    }
}
