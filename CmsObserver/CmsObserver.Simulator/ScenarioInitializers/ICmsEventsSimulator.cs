using CmsObserver.Simulator.Entities;

namespace CmsObserver.Simulator.ScenarioInitializers;

internal interface ICmsEventsSimulator
{
    string MenuTitle { get; }

    IReadOnlyList<CmsEvent> Simulate();
}
