using System.Text.Json.Nodes;
using Goose.CommunityGoals;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class KillCreditTests : IDisposable
    {
        private const int GoalNpc = 44;
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public KillCreditTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private CommunityGoal LoadGoal(long target)
        {
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                PropertiesJson = "{ \"requirements\": [ { \"key\": \"kills\", \"type\": \"Kill\", \"npc\": " + GoalNpc + ", \"target\": " + target + " } ] }",
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            return handler.Goals[1];
        }

        private CommunityGoalRun LoadRun()
        {
            var run = new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = started };
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return run;
        }

        private Player PlayerOn(Map map, int id, int x, int y, int level = 10)
        {
            return new Player(0)
            {
                PlayerID = id,
                Map = map, MapID = map.ID, MapX = x, MapY = y,
                BaseStats = new AttributeSet(),
                MaxStats = new AttributeSet(),
                Class = world.ClassHandler.GetClass(0)!,
                Level = level,
            };
        }

        private NPC SpawnNpc(Map map, int templateId, int x, int y)
        {
            var template = new NPCTemplate
            {
                NPCTemplateID = templateId,
                Name = "Npc" + templateId,
                Level = 10,
                ClassID = 0,
                CanBeKilled = true,
                BaseStats = new AttributeSet { HP = 100 },
                Drops = [],
            };
            return world.NPCHandler.SpawnNPC(world, map.ID, x, y, template, false)!;
        }

        private static Group GroupOf(params Player[] players)
        {
            var group = new Group();
            foreach (var p in players)
            {
                p.Group = group;
                group.Players.Add(p);
            }
            return group;
        }

        [Fact]
        public void SoloKill_CreditsTotalOnceAndKiller()
        {
            var goal = LoadGoal(100);
            var run = LoadRun();
            var map = fixture.AddBaseMap(1, "m", 10, 10);
            var npc = SpawnNpc(map, GoalNpc, 5, 5);
            var killer = PlayerOn(map, 1, 5, 6);

            npc.Attacked(killer, 100, world);

            Assert.Equal(1, run.Totals["kills"]);
            Assert.Equal(1, run.Contributions[1].Amounts["kills"]);
            Assert.Equal(1, run.Contributions.Count);
            Assert.True(handler.IsRunDirty(1, 1));
            Assert.True(handler.IsContributionDirty(1, 1, 1));
        }

        [Fact]
        public void GroupKill_CreditsTotalOnceAndEachEligibleMember()
        {
            var goal = LoadGoal(100);
            var run = LoadRun();
            var map = fixture.AddBaseMap(1, "m", 40, 40);
            var npc = SpawnNpc(map, GoalNpc, 25, 25);
            var eligible1 = PlayerOn(map, 1, 25, 26);
            var eligible2 = PlayerOn(map, 2, 26, 25);
            var eligible3 = PlayerOn(map, 3, 24, 25);
            var far = PlayerOn(map, 4, 0, 0);
            GroupOf(eligible1, eligible2, eligible3, far);

            npc.Attacked(eligible1, 40, world);
            npc.Attacked(eligible2, 30, world);
            npc.Attacked(eligible3, 20, world);
            npc.Attacked(far, 10, world);

            Assert.Equal(1, run.Totals["kills"]);
            Assert.Equal(1, run.Contributions[1].Amounts["kills"]);
            Assert.Equal(1, run.Contributions[2].Amounts["kills"]);
            Assert.Equal(1, run.Contributions[3].Amounts["kills"]);
            Assert.False(run.Contributions.ContainsKey(4));
            Assert.True(handler.IsContributionDirty(1, 1, 1));
            Assert.True(handler.IsContributionDirty(1, 1, 2));
            Assert.True(handler.IsContributionDirty(1, 1, 3));
            Assert.False(handler.IsContributionDirty(1, 1, 4));
        }

        [Fact]
        public void PetKill_CreditsOwnerNotPet()
        {
            var goal = LoadGoal(100);
            var run = LoadRun();
            var map = fixture.AddBaseMap(1, "m", 10, 10);
            var npc = SpawnNpc(map, GoalNpc, 5, 5);
            var owner = PlayerOn(map, 1, 5, 6);
            var pet = new Pet
            {
                PlayerID = 2,
                Name = "Pet",
                Owner = owner,
                Map = map, MapID = map.ID, MapX = 5, MapY = 6,
                BaseStats = new AttributeSet(),
                MaxStats = new AttributeSet(),
                Class = world.ClassHandler.GetClass(0)!,
                Level = 10,
            };

            npc.Attacked(pet, 100, world);

            Assert.Equal(1, run.Totals["kills"]);
            Assert.Equal(1, run.Contributions[1].Amounts["kills"]);
            Assert.False(run.Contributions.ContainsKey(2));
        }

        [Fact]
        public void NonGoalNpcKill_LeavesAllGoalsUntouched()
        {
            var goal = LoadGoal(100);
            var run = LoadRun();
            var map = fixture.AddBaseMap(1, "m", 10, 10);
            var npc = SpawnNpc(map, 55, 5, 5);
            var killer = PlayerOn(map, 1, 5, 6);

            npc.Attacked(killer, 100, world);

            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.False(handler.IsRunDirty(1, 1));
            Assert.False(handler.IsContributionDirty(1, 1, 1));
        }

        [Fact]
        public void KillAfterKillTargetMet_StillCreditsPlayers()
        {
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                PropertiesJson = "{ \"requirements\": [ { \"key\": \"kills\", \"type\": \"Kill\", \"npc\": " + GoalNpc + ", \"target\": 5 }, { \"key\": \"gold\", \"type\": \"Gold\", \"target\": 1000 } ] }",
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            var run = new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = started };
            run.Totals["kills"] = 5;
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            var map = fixture.AddBaseMap(1, "m", 10, 10);
            var npc = SpawnNpc(map, GoalNpc, 5, 5);
            var killer = PlayerOn(map, 1, 5, 6);

            npc.Attacked(killer, 100, world);

            Assert.Equal(5, run.Totals["kills"]);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.Equal(1, run.Contributions[1].Amounts["kills"]);
        }

        [Fact]
        public void CompletingKill_KillerAppearsInResultsPlaces()
        {
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                PropertiesJson = "{ \"requirements\": [ { \"key\": \"kills\", \"type\": \"Kill\", \"npc\": " + GoalNpc + ", \"target\": 1 } ], \"brackets\": [ { \"rank\": 1 }, { \"minScore\": 0 } ] }",
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            var run = LoadRun();
            var map = fixture.AddBaseMap(1, "m", 10, 10);
            var npc = SpawnNpc(map, GoalNpc, 5, 5);
            var killer = PlayerOn(map, 1, 5, 6);

            npc.Attacked(killer, 100, world);

            Assert.Equal(1, run.Totals["kills"]);
            Assert.Equal(RunStatus.Completed, run.Status);
            var places = ((JsonArray)run.Results!["places"]!).Select(n => n!.GetValue<int>()).ToList();
            Assert.Contains(1, places);
        }

        [Fact]
        public void GroupKill_QuestKillProgressUnchanged()
        {
            var map = fixture.AddBaseMap(1, "m", 40, 40);
            var npc = SpawnNpc(map, GoalNpc, 25, 25);
            var quest = new Quest
            {
                Id = 1,
                Name = "Quest",
                Description = "desc",
                FailText = "fail",
                PassText = "pass",
            };
            var requirement = new QuestRequirement
            {
                Id = 1,
                Type = RequirementType.Kill,
                Value = GoalNpc,
                Value2 = 5,
                Quest = quest,
            };
            quest.Requirements.Add(requirement);
            var eligible1 = PlayerOn(map, 1, 25, 26);
            var eligible2 = PlayerOn(map, 2, 26, 25);
            var eligible3 = PlayerOn(map, 3, 24, 25);
            var far = PlayerOn(map, 4, 0, 0);
            GroupOf(eligible1, eligible2, eligible3, far);
            foreach (var p in new[] { eligible1, eligible2, eligible3, far })
            {
                p.QuestsStarted.Add(quest);
                p.QuestProgress.Add(new QuestProgress { Requirement = requirement, Value = 2 });
            }

            npc.Attacked(eligible1, 40, world);
            npc.Attacked(eligible2, 30, world);
            npc.Attacked(eligible3, 20, world);
            npc.Attacked(far, 10, world);

            Assert.Equal(3, eligible1.QuestProgress[0].Value);
            Assert.Equal(3, eligible2.QuestProgress[0].Value);
            Assert.Equal(3, eligible3.QuestProgress[0].Value);
            Assert.Equal(2, far.QuestProgress[0].Value);
        }
    }
}
