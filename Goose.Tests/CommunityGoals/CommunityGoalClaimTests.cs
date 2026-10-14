using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.CommunityGoals
{
    public class CommunityGoalClaimTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly TestWorldFixture.CapturingPlayer player;
        private readonly NPC npc;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public CommunityGoalClaimTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            var map = fixture.AddBaseMap(1, "m", 10, 10);
            player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            player.Spellbook = new Spellbook(player, fixture.Settings);
            map.AddPlayer(player, world);
            npc = new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 7, Name = "Trainer" } };
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private static string ScriptProps() => """
            { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
              "brackets": [ { "minScore": 0, "rewards": [
                  { "type": "Script", "script": "Scripts/CommunityGoal/R.csx" } ] } ] }
            """;

        private static string PersonalProps() => """
            { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
              "brackets": [ { "minScore": 0, "rewards": [
                  { "type": "Item", "value": 900, "value2": 2 },
                  { "type": "Title", "value": "Master Builder" } ] } ] }
            """;

        private CommunityGoal LoadGoal(string properties, int id = 1, int npcTemplateId = 7)
        {
            var goal = new CommunityGoal
            {
                Id = id,
                Name = "Goal " + id,
                NpcTemplateId = npcTemplateId,
                PropertiesJson = properties,
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            return handler.Goals[id];
        }

        private CommunityGoalRun LoadRun(RunStatus status, DateTime? completedAt,
                                         params (int Id, long Amount)[] contributors)
        {
            var run = new CommunityGoalRun
            {
                GoalId = 1,
                Run = 1,
                StartedAt = started,
                Status = status,
                CompletedAt = completedAt,
            };
            foreach (var (id, amount) in contributors)
            {
                var contribution = new Contribution { PlayerId = id, LastAt = started };
                contribution.Amounts["gold"] = amount;
                run.Contributions[id] = contribution;
                run.Totals["gold"] = run.Totals.GetValueOrDefault("gold") + amount;
            }
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return run;
        }

        private (CommunityGoalRun Run1, CommunityGoalRun Run2) LoadTwoRuns()
        {
            var run1 = new CommunityGoalRun
            {
                GoalId = 1,
                Run = 1,
                StartedAt = started,
                Status = RunStatus.Completed,
                CompletedAt = started,
            };
            var contribution = new Contribution { PlayerId = player.PlayerID, LastAt = started };
            contribution.Amounts["gold"] = 50;
            run1.Contributions[player.PlayerID] = contribution;
            run1.Totals["gold"] = 50;
            var run2 = new CommunityGoalRun
            {
                GoalId = 1,
                Run = 2,
                StartedAt = started,
                Status = RunStatus.Active,
            };
            handler.LoadStateRows(new[] { run1, run2 }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return (run1, run2);
        }

        private void RegisterItem(int id, string name, int stackSize = 1) =>
            world.ItemHandler.AddTemplate(new ItemTemplate
            {
                ID = id, Name = name, Description = name, StackSize = stackSize,
                BaseStats = new AttributeSet(), ScriptParams = "",
            });

        private void CompileRewardScript(string body, string fileName = "R.csx")
        {
            Directory.CreateDirectory(Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal"));
            File.WriteAllText(Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal", fileName), body);
        }

        private void FillInventory()
        {
            RegisterItem(950, "Filler");
            var filler = world.ItemHandler.GetTemplate(950)!;
            for (var i = 1; i <= fixture.Settings.InventorySize; i++)
                player.Inventory.SetSlot(i, new ItemSlot { Item = LoadItem(filler) });
        }

        private static Item LoadItem(ItemTemplate template)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            return item;
        }

        private int ItemStack(string name) =>
            player.Inventory.GetInventorySlots().Where(s => s?.Item.Name == name).Sum(s => (int)s!.Stack);

        [Fact]
        public void Claim_HappyPath_GrantsRewardsAndMarksClaimed()
        {
            var goal = LoadGoal(PersonalProps());
            RegisterItem(900, "Sword", stackSize: 10);
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            var contribution = run.Contributions[player.PlayerID];

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.True(result.Success);
            Assert.Null(result.Refusal);
            Assert.Equal(2, ItemStack("Sword"));
            Assert.Equal("Master Builder", player.Title);
            Assert.Equal(0, contribution.ClaimedBracket);
            Assert.Equal(now, contribution.ClaimedAt);
            Assert.True(handler.IsContributionDirty(1, 1, player.PlayerID));
        }

        [Fact]
        public void Claim_ActiveRun_Refused()
        {
            var goal = LoadGoal(PersonalProps());
            var run = LoadRun(RunStatus.Active, null, (player.PlayerID, 50));

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("This goal hasn't finished yet.", result.Refusal);
            Assert.Null(run.Contributions[player.PlayerID].ClaimedBracket);
            Assert.False(handler.IsContributionDirty(1, 1, player.PlayerID));
        }

        [Fact]
        public void Claim_FailedRun_Refused()
        {
            var goal = LoadGoal(PersonalProps());
            var run = LoadRun(RunStatus.Failed, started, (player.PlayerID, 50));

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("This goal hasn't finished yet.", result.Refusal);
        }

        [Fact]
        public void Claim_NoContribution_Refused()
        {
            var goal = LoadGoal(PersonalProps());
            var run = LoadRun(RunStatus.Completed, started, (42, 50));

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("You have nothing to claim.", result.Refusal);
        }

        [Fact]
        public void Claim_NoBracket_Refused()
        {
            var goal = LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "brackets": [ { "minScore": 100, "rewards": [ { "type": "Gold", "value": 50 } ] } ] }
                """);
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 10));

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("You have nothing to claim.", result.Refusal);
            Assert.Equal(0, player.Gold);
            Assert.Null(run.Contributions[player.PlayerID].ClaimedBracket);
        }

        [Fact]
        public void Claim_AlreadyClaimed_Refused()
        {
            var goal = LoadGoal(PersonalProps());
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            run.Contributions[player.PlayerID].ClaimedBracket = 0;

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("You've already claimed this reward.", result.Refusal);
            Assert.Equal(0, ItemStack("Sword"));
            Assert.False(handler.IsContributionDirty(1, 1, player.PlayerID));
        }

        [Fact]
        public void Claim_Expired_Refused()
        {
            var goal = LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "repeat": "onClose", "claimWindow": "7d",
                  "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 50 } ] } ] }
                """);
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            now = started.AddDays(8);

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("This reward has expired.", result.Refusal);
            Assert.Equal(0, player.Gold);
        }

        [Fact]
        public void Claim_WithinClaimWindow_NotExpired()
        {
            var goal = LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "repeat": "onClose", "claimWindow": "7d",
                  "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 50 } ] } ] }
                """);
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            now = started.AddDays(7);

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.True(result.Success);
            Assert.Equal(50, player.Gold);
        }

        [Fact]
        public void Claim_GoalRemoved_Refused()
        {
            var goal = LoadGoal(PersonalProps());
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            LoadGoal(PersonalProps(), id: 2);

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("This reward has expired.", result.Refusal);
            Assert.Equal(0, ItemStack("Sword"));
        }

        [Fact]
        public void Claim_ScriptRefusal_ReturnsScriptMessageVerbatim()
        {
            CompileRewardScript(@"
public class R : BaseCommunityRewardScript
{
    public override string? CanGive(CommunityRewardContext ctx) => ""no room for you"";
}
return typeof(R);");
            var goal = LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "brackets": [ { "minScore": 0, "rewards": [
                      { "type": "Script", "script": "Scripts/CommunityGoal/R.csx" } ] } ] }
                """);
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("no room for you", result.Refusal);
            Assert.Null(run.Contributions[player.PlayerID].ClaimedBracket);
        }

        [Fact]
        public void Claim_InsufficientSpace_RefusesAndGrantsNothingFromTheBracket()
        {
            var goal = LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "brackets": [ { "minScore": 0, "rewards": [
                      { "type": "Item", "value": 900 },
                      { "type": "Gold", "value": 500 },
                      { "type": "Title", "value": "Master Builder" } ] } ] }
                """);
            RegisterItem(900, "Sword");
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            FillInventory();

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("You need 1 free inventory slots to claim this.", result.Refusal);
            Assert.Equal(0, player.Gold);
            Assert.Null(player.Title);
            Assert.Equal(0, ItemStack("Sword"));
            Assert.Null(run.Contributions[player.PlayerID].ClaimedBracket);
            Assert.Null(run.Contributions[player.PlayerID].ClaimedAt);
            Assert.False(handler.IsContributionDirty(1, 1, player.PlayerID));
            Assert.Equal(0, player.Inventory.GetNumberOfFreeSlots());
        }

        [Fact]
        public void Claim_DoubleClaim_SecondRefused()
        {
            var goal = LoadGoal(PersonalProps());
            RegisterItem(900, "Sword", stackSize: 10);
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));

            var first = handler.Claim(goal, run, player, npc, world);
            var second = handler.Claim(goal, run, player, npc, world);

            Assert.True(first.Success);
            Assert.False(second.Success);
            Assert.Equal("You've already claimed this reward.", second.Refusal);
            Assert.Equal(2, ItemStack("Sword"));
            Assert.Equal(0, run.Contributions[player.PlayerID].ClaimedBracket);
        }

        [Fact]
        public void Claim_RunOneClaimableWhileRunTwoActive()
        {
            var goal = LoadGoal(PersonalProps());
            RegisterItem(900, "Sword", stackSize: 10);
            var (run1, run2) = LoadTwoRuns();
            Assert.Equal(RunStatus.Active, run2.Status);

            var result = handler.Claim(goal, run1, player, npc, world);

            Assert.True(result.Success);
            Assert.Equal(2, ItemStack("Sword"));
            Assert.Equal(0, run1.Contributions[player.PlayerID].ClaimedBracket);
            Assert.Equal(now, run1.Contributions[player.PlayerID].ClaimedAt);
            Assert.True(handler.IsContributionDirty(1, 1, player.PlayerID));
            Assert.False(handler.IsContributionDirty(1, 2, player.PlayerID));
        }

        [Fact]
        public void PendingPrizes_ListsOnlyThisNpcsGoals()
        {
            var goal1 = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                NpcTemplateId = 7,
                PropertiesJson = PersonalProps(),
            };
            var goal2 = new CommunityGoal
            {
                Id = 2,
                Name = "Goal 2",
                NpcTemplateId = 8,
                PropertiesJson = PersonalProps(),
            };
            handler.LoadDefinitionRows(new[] { goal1, goal2 }, world);
            RegisterItem(900, "Sword", stackSize: 10);
            var run1 = new CommunityGoalRun
            {
                GoalId = 1, Run = 1, StartedAt = started,
                Status = RunStatus.Completed, CompletedAt = started,
            };
            var contribution = new Contribution { PlayerId = player.PlayerID, LastAt = started };
            contribution.Amounts["gold"] = 50;
            run1.Contributions[player.PlayerID] = contribution;
            run1.Totals["gold"] = 50;
            var run2 = new CommunityGoalRun
            {
                GoalId = 2, Run = 1, StartedAt = started,
                Status = RunStatus.Completed, CompletedAt = started,
            };
            var contribution2 = new Contribution { PlayerId = player.PlayerID, LastAt = started };
            contribution2.Amounts["gold"] = 50;
            run2.Contributions[player.PlayerID] = contribution2;
            run2.Totals["gold"] = 50;
            handler.LoadStateRows(new[] { run1, run2 }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());

            var prizes = handler.PendingPrizes(player, 7);

            var prize = Assert.Single(prizes);
            Assert.Equal(1, prize.Goal.Id);
            Assert.Equal(1, prize.Run.Run);
            Assert.Equal(0, prize.BracketIndex);

            var other = handler.PendingPrizes(player, 8);
            var otherPrize = Assert.Single(other);
            Assert.Equal(2, otherPrize.Goal.Id);
        }

        [Fact]
        public void PendingPrizes_ExcludesClaimedExpiredAndNonContributorRuns()
        {
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                NpcTemplateId = 7,
                PropertiesJson = """
                    { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                      "repeat": "onClose", "claimWindow": "7d",
                      "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 50 } ] } ] }
                    """,
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            now = started.AddDays(8);
            var fresh = new CommunityGoalRun
            {
                GoalId = 1, Run = 1, StartedAt = now,
                Status = RunStatus.Completed, CompletedAt = now,
            };
            var freshContribution = new Contribution { PlayerId = player.PlayerID, LastAt = now };
            freshContribution.Amounts["gold"] = 50;
            fresh.Contributions[player.PlayerID] = freshContribution;
            fresh.Totals["gold"] = 50;
            var expired = new CommunityGoalRun
            {
                GoalId = 1, Run = 2, StartedAt = started.AddDays(-20),
                Status = RunStatus.Completed, CompletedAt = started.AddDays(-20),
            };
            var expiredContribution = new Contribution { PlayerId = player.PlayerID, LastAt = started.AddDays(-20) };
            expiredContribution.Amounts["gold"] = 50;
            expired.Contributions[player.PlayerID] = expiredContribution;
            expired.Totals["gold"] = 50;
            var claimed = new CommunityGoalRun
            {
                GoalId = 1, Run = 3, StartedAt = started,
                Status = RunStatus.Completed, CompletedAt = started,
            };
            var claimedContribution = new Contribution { PlayerId = player.PlayerID, LastAt = started };
            claimedContribution.Amounts["gold"] = 50;
            claimedContribution.ClaimedBracket = 0;
            claimed.Contributions[player.PlayerID] = claimedContribution;
            claimed.Totals["gold"] = 50;
            var otherPlayer = new CommunityGoalRun
            {
                GoalId = 1, Run = 4, StartedAt = started,
                Status = RunStatus.Completed, CompletedAt = started,
            };
            var otherContribution = new Contribution { PlayerId = 42, LastAt = started };
            otherContribution.Amounts["gold"] = 50;
            otherPlayer.Contributions[42] = otherContribution;
            otherPlayer.Totals["gold"] = 50;
            handler.LoadStateRows(new[] { fresh, expired, claimed, otherPlayer },
                Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());

            var prizes = handler.PendingPrizes(player, 7);

            var prize = Assert.Single(prizes);
            Assert.Equal(1, prize.Run.Run);
        }

        [Fact]
        public void Claim_ScriptCanGiveThrows_RefusesFailClosed()
        {
            CompileRewardScript(@"
public class R : BaseCommunityRewardScript
{
    public override string? CanGive(CommunityRewardContext ctx) => throw new InvalidOperationException(""boom"");
}
return typeof(R);");
            var goal = LoadGoal(ScriptProps());
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            using var log = new CapturingLog();

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("You cannot claim this reward right now.", result.Refusal);
            Assert.Null(run.Contributions[player.PlayerID].ClaimedBracket);
            Assert.False(handler.IsContributionDirty(1, 1, player.PlayerID));
            Assert.Contains(log.Messages, m => m.Contains("CanGive Exception"));
        }

        [Fact]
        public void Claim_ScriptGiveThrows_RefusesWithoutClaimFields()
        {
            CompileRewardScript(@"
public class R : BaseCommunityRewardScript
{
    public override void Give(CommunityRewardContext ctx) => throw new InvalidOperationException(""boom"");
}
return typeof(R);");
            var goal = LoadGoal(ScriptProps());
            var run = LoadRun(RunStatus.Completed, started, (player.PlayerID, 50));
            var contribution = run.Contributions[player.PlayerID];
            using var log = new CapturingLog();

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("You cannot claim this reward right now.", result.Refusal);
            Assert.Null(contribution.ClaimedBracket);
            Assert.Null(contribution.ClaimedAt);
            Assert.False(handler.IsContributionDirty(1, 1, player.PlayerID));
            Assert.Contains(log.Messages, m => m.Contains("Give Exception"));
        }

        [Fact]
        public void Claim_MismatchedGoalRun_Refused()
        {
            var goal = LoadGoal(PersonalProps());
            RegisterItem(900, "Sword", stackSize: 10);
            var run = new CommunityGoalRun
            {
                GoalId = 2,
                Run = 1,
                StartedAt = started,
                Status = RunStatus.Completed,
                CompletedAt = started,
            };
            var contribution = new Contribution { PlayerId = player.PlayerID, LastAt = started };
            contribution.Amounts["gold"] = 50;
            run.Contributions[player.PlayerID] = contribution;
            run.Totals["gold"] = 50;
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());

            var result = handler.Claim(goal, run, player, npc, world);

            Assert.False(result.Success);
            Assert.Equal("This goal hasn't finished yet.", result.Refusal);
            Assert.Equal(0, ItemStack("Sword"));
            Assert.Null(contribution.ClaimedBracket);
        }
    }
}
