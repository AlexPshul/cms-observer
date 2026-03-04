using System.Text.Json;

namespace CmsObserver.API.Dtos;

public sealed record CmsEntityDto(
    string Id,
    int Version,
    JsonElement Payload,
    DateTimeOffset TimestampUtc,
    bool IsActive);
