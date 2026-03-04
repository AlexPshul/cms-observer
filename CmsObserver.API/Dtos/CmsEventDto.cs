using System.Text.Json;

namespace CmsObserver.API.Dtos;

public sealed record CmsEventDto(
    CmsEventDtoType Type,
    string Id,
    DateTimeOffset Timestamp,
    JsonElement? Payload,
    int? Version);
