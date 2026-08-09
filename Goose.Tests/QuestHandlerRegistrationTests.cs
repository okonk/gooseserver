using Goose.Quests;

namespace Goose.Tests;

public class QuestHandlerRegistrationTests
{
    [Fact]
    public void Registered_quests_are_retrievable_by_id()
    {
        var handler = new QuestHandler();
        var quest = new Quest { Id = 900001, Name = "Abysmal Terror (1)" };

        handler.AddQuest(quest);

        Assert.NotNull(quest.PrerequisiteQuests);
        Assert.Same(quest, handler.Get(900001));
        Assert.Same(quest, handler.Quests[900001]);
    }

    [Fact]
    public void Unknown_ids_return_null()
    {
        Assert.Null(new QuestHandler().Get(900001));
    }
}
