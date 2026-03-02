using CmsObserver.Simulator.Entities;

namespace CmsObserver.Simulator.ScenarioInitializers;

internal sealed class SpiderManErasSimulator : ICmsEventsSimulator
{
    public string MenuTitle => "Spider-Man: The Three Eras (Multiple revisions of a single entity)";

    public IReadOnlyList<CmsEvent> Simulate()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            // Tobey Maguire era
            CmsEventFactory.Publish("spider-man", 1, now, "Spider-Man", "Tobey Maguire", ["Spider-Man"]),
            CmsEventFactory.Publish("spider-man", 2, now.AddSeconds(1), "Spider-Man", "Tobey Maguire", ["Spider-Man", "Spider-Man 2"]),
            CmsEventFactory.Publish("spider-man", 3, now.AddSeconds(2), "Spider-Man", "Tobey Maguire", ["Spider-Man", "Spider-Man 2", "Spider-Man 3"]),

            // Andrew Garfield era
            CmsEventFactory.Publish("spider-man", 4, now.AddSeconds(3), "Spider-Man", "Andrew Garfield", ["The Amazing Spider-Man"]),
            CmsEventFactory.Publish("spider-man", 5, now.AddSeconds(4), "Spider-Man", "Andrew Garfield", ["The Amazing Spider-Man", "The Amazing Spider-Man 2"]),

            // Tom Holland era
            CmsEventFactory.Publish("spider-man", 6, now.AddSeconds(5), "Spider-Man", "Tom Holland", ["Spider-Man: Homecoming"]),
            CmsEventFactory.Publish("spider-man", 7, now.AddSeconds(6), "Spider-Man", "Tom Holland", ["Spider-Man: Homecoming", "Spider-Man: Far From Home"]),
            CmsEventFactory.Publish("spider-man", 8, now.AddSeconds(7), "Spider-Man", "Tom Holland", ["Spider-Man: Homecoming", "Spider-Man: Far From Home", "Spider-Man: No Way Home"]),
        ];
    }
}
