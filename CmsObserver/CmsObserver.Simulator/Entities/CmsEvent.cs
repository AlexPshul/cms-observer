namespace CmsObserver.Simulator.Entities;

internal record CmsEvent(CmsEventType Type, string Id, Dictionary<string, object>? Payload, int? Version, DateTimeOffset Timestamp);