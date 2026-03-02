using CmsObserver.Simulator.Entities;

namespace CmsObserver.Simulator.ScenarioInitializers;

internal sealed class TchallaCornerCaseSimulator : ICmsEventsSimulator
{
    public string MenuTitle => "T'Challa: The Fallen King (Unpublish with unseen version)";

    public IReadOnlyList<CmsEvent> Simulate()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            CmsEventFactory.Publish("tchalla", 1, now, "Black Panther", "Chadwick Boseman", ["Black Panther"]),
            CmsEventFactory.Unpublish("tchalla", 2, now.AddSeconds(1), "Black Panther", "Chadwick Boseman", ["Black Panther", "Black Panther II (Cancelled)"])
        ];
    }
}
