using CmsObserver.Simulator.Entities;

namespace CmsObserver.Simulator.ScenarioInitializers;

internal sealed class KangDynastySimulator : ICmsEventsSimulator
{
    public string MenuTitle => "The Kang Dynasty Collapses (Hard delete multiple entities)";

    public IReadOnlyList<CmsEvent> Simulate()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            CmsEventFactory.Publish("he-who-remains", 1, now, "He Who Remains", "Jonathan Majors", ["Loki: Season 1"]),
            CmsEventFactory.Publish("kang-the-conqueror", 1, now.AddSeconds(1), "Kang the Conqueror", "Jonathan Majors", ["Ant-Man and the Wasp: Quantumania"]),
            CmsEventFactory.Publish("immortus", 1, now.AddSeconds(2), "Immortus", "Jonathan Majors", ["Ant-Man and the Wasp: Quantumania"]),

            CmsEventFactory.Delete("he-who-remains", now.AddSeconds(3)),
            CmsEventFactory.Delete("kang-the-conqueror", now.AddSeconds(4)),
            CmsEventFactory.Delete("immortus", now.AddSeconds(5))
        ];
    }
}
