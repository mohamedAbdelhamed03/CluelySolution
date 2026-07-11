namespace Cluely.Application.Rooms.SelectDictionary;

public sealed record SelectDictionaryCommand(Guid RoomId, Guid ParticipantId, string RegionCode, string ContentVersion, Guid CorrelationId);