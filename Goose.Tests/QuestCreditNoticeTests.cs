using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestCreditNoticeTests
{
    private sealed class Fixture : IDisposable
    {
        public TestWorldFixture World { get; }
        public NPC Npc { get; }
        public TestWorldFixture.CapturingPlayer Player { get; }
        public Quest Quest { get; }

        public Fixture()
        {
            this.World = new TestWorldFixture();
            this.Npc = new NPC
            {
                NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Trainer" },
            };
            this.Player = new TestWorldFixture.CapturingPlayer
            {
                Class = this.World.World.ClassHandler.GetClass(0)!,
            };
            this.Quest = new Quest
            {
                Id = 1,
                Name = "Q",
                Description = "desc",
                FailText = "fail",
                PassText = "pass",
            };
            var requirement = new QuestRequirement { Id = 1, Quest = this.Quest, Type = RequirementType.Kill, Value = 5, Value2 = 3 };
            this.Quest.Requirements.Add(requirement);
            this.Player.QuestsStarted.Add(this.Quest);
            this.Player.QuestProgress.Add(new QuestProgress { Requirement = requirement, Value = 0 });
        }

        public void Dispose() => this.World.Dispose();
    }

    [Fact]
    public void Killed_WithKillProgress_SendsQuestNotice()
    {
        using var fixture = new Fixture();

        fixture.Player.Killed(fixture.Npc, fixture.World.World);

        Assert.Contains(fixture.Player.Sent, s => s.StartsWith(P.Notice(NoticeType.Quest, "Quest Credit: Q (1/3)")));
        Assert.DoesNotContain(fixture.Player.Sent, s => s.StartsWith("BT"));
    }

    [Fact]
    public void Killed_WithQuestCreditFilter_SendsNothing()
    {
        using var fixture = new Fixture();
        fixture.Player.ToggleSettings |= Player.ToggleSetting.QuestCredit;

        fixture.Player.Killed(fixture.Npc, fixture.World.World);

        Assert.DoesNotContain(fixture.Player.Sent, s => s.StartsWith("NTF"));
        var progress = Assert.Single(fixture.Player.QuestProgress);
        Assert.Equal(1, progress.Value);
    }
}
