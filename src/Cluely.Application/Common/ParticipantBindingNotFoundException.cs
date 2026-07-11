namespace Cluely.Application.Common;

public sealed class ParticipantBindingNotFoundException : Exception
{
    public const string ErrorCode = "ParticipantBindingNotFound";

    public ParticipantBindingNotFoundException()
        : base("Participant binding not found for the current user in this room.")
    {
    }
}
