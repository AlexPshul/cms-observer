using CmsObserver.Simulator.Entities;

namespace CmsObserver.Simulator.ScenarioInitializers;

internal sealed class DeadpoolRebootSimulator : ICmsEventsSimulator
{
    public string MenuTitle => "Deadpool: They Tried To Silence Me (Delete and re-publish as v1)";

    public IReadOnlyList<CmsEvent> Simulate()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            CmsEventFactory.Publish("deadpool", 1, now, "Silent Deadpool (Fox)", "Ryan Reynolds", ["X-Men Origins: Wolverine"]),
            CmsEventFactory.Delete("deadpool", now.AddSeconds(1)),
            CmsEventFactory.Publish("deadpool", 1, now.AddSeconds(2), "Deadpool", "Ryan Reynolds", ["Deadpool"])
        ];
    }
}
