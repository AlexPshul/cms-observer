using CmsObserver.Simulator.Entities;

namespace CmsObserver.Simulator.ScenarioInitializers;

internal sealed class AvengersAssembleSimulator : ICmsEventsSimulator
{
    public string MenuTitle => "Avengers Assemble! (Initialize and update entities)";

    public IReadOnlyList<CmsEvent> Simulate()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            // Iron Man (2008)
            CmsEventFactory.Publish("iron-man", 1, now, "Iron Man", "Robert Downey Jr.", ["Iron Man"]),

            // The Incredible Hulk (2008)
            CmsEventFactory.Publish("hulk", 1, now.AddSeconds(1), "Hulk", "Edward Norton", ["The Incredible Hulk"]),

            // Iron Man 2 (2010)
            CmsEventFactory.Publish("iron-man", 2, now.AddSeconds(2), "Iron Man", "Robert Downey Jr.", ["Iron Man", "Iron Man 2"]),
            CmsEventFactory.Publish("black-widow", 1, now.AddSeconds(3), "Black Widow", "Scarlett Johansson", ["Iron Man 2"]),

            // Thor (2011)
            CmsEventFactory.Publish("thor", 1, now.AddSeconds(4), "Thor", "Chris Hemsworth", ["Thor"]),
            CmsEventFactory.Publish("hawkeye", 1, now.AddSeconds(5), "Hawkeye", "Jeremy Renner", ["Thor"]),

            // Captain America: The First Avenger (2011)
            CmsEventFactory.Publish("captain-america", 1, now.AddSeconds(6), "Captain America", "Chris Evans", ["Captain America: The First Avenger"]),

            // The Avengers (2012) — everyone gets a version bump
            CmsEventFactory.Publish("iron-man", 3, now.AddSeconds(7), "Iron Man", "Robert Downey Jr.", ["Iron Man", "Iron Man 2", "The Avengers"]),
            CmsEventFactory.Publish("hulk", 2, now.AddSeconds(8), "Hulk", "Mark Ruffalo", ["The Incredible Hulk", "The Avengers"]),
            CmsEventFactory.Publish("black-widow", 2, now.AddSeconds(9), "Black Widow", "Scarlett Johansson", ["Iron Man 2", "The Avengers"]),
            CmsEventFactory.Publish("thor", 2, now.AddSeconds(10), "Thor", "Chris Hemsworth", ["Thor", "The Avengers"]),
            CmsEventFactory.Publish("hawkeye", 2, now.AddSeconds(11), "Hawkeye", "Jeremy Renner", ["Thor", "The Avengers"]),
            CmsEventFactory.Publish("captain-america", 2, now.AddSeconds(12), "Captain America", "Chris Evans", ["Captain America: The First Avenger", "The Avengers"])
        ];
    }
}
