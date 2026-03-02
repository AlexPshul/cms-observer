namespace CmsObserver.Simulator.Entities;

internal static class CmsEventFactory
{
    internal static CmsEvent Publish(string id, int version, DateTimeOffset timestamp, string name, string actor, string[] movies) =>
        new(CmsEventType.Published, id, BuildPayload(name, actor, movies), version, timestamp);

    internal static CmsEvent Unpublish(string id, int version, DateTimeOffset timestamp, string name, string actor, string[] movies) =>
        new(CmsEventType.Unpublished, id, BuildPayload(name, actor, movies), version, timestamp);

    internal static CmsEvent Delete(string id, DateTimeOffset timestamp) =>
        new(CmsEventType.Deleted, id, null, null, timestamp);

    private static Dictionary<string, object> BuildPayload(string name, string actor, string[] movies) => new()
    {
        ["name"] = name,
        ["actor"] = actor,
        ["movies"] = movies
    };
}
