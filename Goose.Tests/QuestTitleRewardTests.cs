using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestTitleRewardTests
{
    private sealed class Fixture : IDisposable
    {
        public TestWorldFixture World { get; }
        public Map Map { get; }
        public NPC Npc { get; }
        public TestWorldFixture.CapturingPlayer Player { get; }
        public TestWorldFixture.CapturingPlayer Other { get; }
        public Quest Quest { get; }

        public Fixture(bool repeatable = false)
        {
            this.World = new TestWorldFixture();
            this.Map = this.World.AddBaseMap(1, "m", 60, 40);
            this.Npc = new NPC
            {
                NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Trainer" },
            };
            this.Player = this.World.CommandPlayerOn(this.Map, 5, 5, "Tester");
            this.Player.Level = 5;
            this.Player.Spellbook = new Spellbook(this.Player, this.World.Settings);
            this.Player.LoginID = 101;
            this.Other = this.World.CommandPlayerOn(this.Map, 6, 5, "Other");
            this.Other.LoginID = 102;
            this.Map.AddPlayer(this.Player, this.World.World);
            this.Map.AddPlayer(this.Other, this.World.World);
            this.Quest = new Quest
            {
                Id = 9,
                Name = "Slayer",
                Description = "desc",
                FailText = "fail",
                PassText = "pass",
                MinLevel = 5,
                MaxLevel = 5,
                Repeatable = repeatable,
                ShowProgress = true,
            };
        }

        public void Dispose() => this.World.Dispose();
    }

    private static void CompleteViaWindow(Fixture fixture)
    {
        fixture.Player.QuestsStarted.Add(fixture.Quest);
        var window = new QuestWindow(fixture.Npc, fixture.Player, fixture.Quest, fixture.World.World);
        window.Clicked(Window.ButtonTypes.Next, fixture.Npc.NPCTemplate.NPCTemplateID, 0, 0, fixture.Player, fixture.World.World);
    }

    [Fact]
    public void A_title_reward_unlocks_and_equips_it()
    {
        using var fixture = new Fixture();
        fixture.Quest.Rewards.Add(new QuestReward { Id = 1, Type = RewardType.Title, StringValue = "Dragon Slayer" });

        CompleteViaWindow(fixture);

        Assert.Equal("Dragon Slayer", fixture.Player.Title);
        Assert.Equal(new List<string> { "Dragon Slayer" }, fixture.Player.UnlockedTitles());
    }

    [Fact]
    public void A_title_reward_reaches_nearby_players_immediately()
    {
        using var fixture = new Fixture();
        fixture.Quest.Rewards.Add(new QuestReward { Id = 1, Type = RewardType.Title, StringValue = "Dragon Slayer" });

        CompleteViaWindow(fixture);

        Assert.Contains(fixture.Other.Sent, s => s.StartsWith("MKC" + fixture.Player.LoginID) && s.Contains("Dragon Slayer"));
    }

    [Fact]
    public void A_surname_reward_unlocks_into_the_surname_list()
    {
        using var fixture = new Fixture();
        fixture.Quest.Rewards.Add(new QuestReward { Id = 1, Type = RewardType.Surname, StringValue = "Smith" });

        CompleteViaWindow(fixture);

        Assert.Equal("Smith", fixture.Player.Surname);
        Assert.Equal(new List<string> { "Smith" }, fixture.Player.UnlockedSurnames());
        Assert.Empty(fixture.Player.UnlockedTitles());
    }

    [Fact]
    public void Repeating_a_rewardable_title_does_not_duplicate_the_entry()
    {
        using var fixture = new Fixture(repeatable: true);
        fixture.Quest.Rewards.Add(new QuestReward { Id = 1, Type = RewardType.Title, StringValue = "Dragon Slayer" });

        CompleteViaWindow(fixture);
        CompleteViaWindow(fixture);

        Assert.Equal(new List<string> { "Dragon Slayer" }, fixture.Player.UnlockedTitles());
    }
}
