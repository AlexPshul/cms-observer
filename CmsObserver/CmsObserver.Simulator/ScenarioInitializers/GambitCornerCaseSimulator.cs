using CmsObserver.Simulator.Entities;

namespace CmsObserver.Simulator.ScenarioInitializers;

internal sealed class GambitCornerCaseSimulator : ICmsEventsSimulator
{
    public string MenuTitle => "Gambit: The Project That Never Was (Unpublish non-existent entity)";

    public IReadOnlyList<CmsEvent> Simulate()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            CmsEventFactory.Unpublish("gambit", 2, now, "Gambit", "Channing Tatum", ["Gambit (Cancelled)"])
        ];
    }
}
