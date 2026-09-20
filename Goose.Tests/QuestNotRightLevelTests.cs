using Goose;
using Goose.Quests;
using Goose.Testing;

namespace Goose.Tests;

public class QuestNotRightLevelTests
{
    private static (NPC npc, Player player, Quest quest) Fixture(TestWorldFixture world, int minLevel, long minExperience)
    {
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
        };

        var player = new Player(0);
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);

        var quest = new Quest
        {
            Id = 77,
            Name = "Gated Quest",
            Description = "desc",
            MinLevel = minLevel,
            MinExperience = minExperience,
            Repeatable = false,
        };

        return (npc, player, quest);
    }

    [Fact]
    public void MinLevelOnly_ShowsRequiredLevel()
    {
        using var world = new TestWorldFixture();
        var (npc, player, quest) = Fixture(world, minLevel: 10, minExperience: 0);

        var window = new QuestWindow(npc, player, quest, world.World);

        string text = window.GetCurrentText(player, world.World);

        Assert.Contains("Level 10 required.", text);
        Assert.DoesNotContain("experience required.", text);
    }

    [Fact]
    public void MinExperienceOnly_ShowsRequiredExperience()
    {
        using var world = new TestWorldFixture();
        var (npc, player, quest) = Fixture(world, minLevel: 0, minExperience: 1500000);

        var window = new QuestWindow(npc, player, quest, world.World);

        string text = window.GetCurrentText(player, world.World);

        Assert.Contains("1.5m experience required.", text);
        Assert.DoesNotContain("Level", text);
    }

    [Fact]
    public void BothSet_ShowsBothRequirements()
    {
        using var world = new TestWorldFixture();
        var (npc, player, quest) = Fixture(world, minLevel: 10, minExperience: 2000000);

        var window = new QuestWindow(npc, player, quest, world.World);

        string text = window.GetCurrentText(player, world.World);

        Assert.Contains("Level 10 required.", text);
        Assert.Contains("2m experience required.", text);
    }
}
