using System.Text.Json;

namespace CmsObserver.Managers.Models;

public sealed record CmsEventModel(
    CmsEventModelType Type,
    string Id,
    DateTimeOffset Timestamp,
    JsonElement? Payload,
    int? Version);
