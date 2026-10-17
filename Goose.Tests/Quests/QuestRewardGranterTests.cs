using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Quests;

public class QuestRewardGranterTests
{
    private sealed class Fixture : IDisposable
    {
        public TestWorldFixture World { get; }
        public Map Map { get; }
        public NPC Npc { get; }
        public TestWorldFixture.CapturingPlayer Player { get; }

        public Fixture()
        {
            this.World = new TestWorldFixture();
            this.Map = this.World.AddBaseMap(1, "m", 60, 40);
            this.Npc = new NPC
            {
                NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Trainer" },
            };
            this.Player = this.World.CommandPlayerOn(this.Map, 5, 5, "Tester");
            this.Player.Spellbook = new Spellbook(this.Player, this.World.Settings);
            this.Map.AddPlayer(this.Player, this.World.World);
        }

        public void Dispose() => this.World.Dispose();
    }

    [Fact]
    public void A_gold_reward_adds_gold_and_sends_the_reward_message()
    {
        using var fixture = new Fixture();

        QuestRewardGranter.Give(new QuestReward { Id = 1, Type = RewardType.Gold, LongValue = 500 }, fixture.Npc, fixture.Player, fixture.World.World);

        Assert.Equal(500, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, s => s.StartsWith("NTF1,[Quest Reward]: 500 gold"));
    }

    [Fact]
    public void An_item_reward_adds_a_stacked_item_and_sends_the_reward_message()
    {
        using var fixture = new Fixture();
        fixture.World.World.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 900, Name = "Sword", Description = "Sword", StackSize = 1,
            BaseStats = new AttributeSet(), ScriptParams = "",
        });

        QuestRewardGranter.Give(new QuestReward { Id = 1, Type = RewardType.Item, LongValue = 900, LongValue2 = 2 }, fixture.Npc, fixture.Player, fixture.World.World);

        var slot = Assert.Single(fixture.Player.Inventory.GetInventorySlots(), s => s?.Item.Name == "Sword");
        Assert.Equal(2, slot!.Stack);
        Assert.Contains(fixture.Player.Sent, s => s.Contains("[Quest Reward]: Item: Sword (2)"));
    }

    [Fact]
    public void A_title_reward_unlocks_and_equips_the_title()
    {
        using var fixture = new Fixture();

        QuestRewardGranter.Give(new QuestReward { Id = 1, Type = RewardType.Title, StringValue = "Dragon Slayer" }, fixture.Npc, fixture.Player, fixture.World.World);

        Assert.Equal("Dragon Slayer", fixture.Player.Title);
        Assert.Equal(new List<string> { "Dragon Slayer" }, fixture.Player.UnlockedTitles());
        Assert.Contains(fixture.Player.Sent, s => s.Contains("[Quest Reward]: Title: Dragon Slayer"));
    }
}
