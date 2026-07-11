using FluentValidation;

namespace Cluely.Application.Rooms.SelectDictionary;

public sealed class SelectDictionaryCommandValidator : AbstractValidator<SelectDictionaryCommand>
{
    public SelectDictionaryCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.RegionCode).NotEmpty();
        RuleFor(c => c.ContentVersion).NotEmpty();
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}