using Cluely.Domain.Common;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Room.Entities;

public sealed class Participant
{
    public ParticipantId Id { get; }
    public string Nickname { get; private set; }
    public Team Team { get; private set; }
    public Role Role { get; private set; }
    public bool IsHost { get; private set; }

    private Participant(ParticipantId id, string nickname, bool isHost)
    {
        Id = id;
        Nickname = nickname;
        Team = Team.Unassigned;
        Role = Role.Operative;
        IsHost = isHost;
    }

    // Internal constructor for rehydration
    internal Participant(ParticipantId id, string nickname, Team team, Role role, bool isHost)
    {
        Id = id;
        Nickname = nickname;
        Team = team;
        Role = role;
        IsHost = isHost;
    }

    public static Participant Create(ParticipantId id, string nickname, bool isHost = false) => new(id, nickname, isHost);

    public void SetTeam(Team team) => Team = team;

    public void SetRole(Role role) => Role = role;

    public void SetHost(bool isHost) => IsHost = isHost;
}
