using Cluely.Application.Common.ReadModels;

namespace Cluely.Application.Queries.GetRoomParticipants;

public sealed record GetRoomParticipantsResult(IReadOnlyList<ParticipantReadModel> Participants);
