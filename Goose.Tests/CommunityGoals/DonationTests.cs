using Goose;
using Goose.CommunityGoals;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.CommunityGoals
{
    public class DonationTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly TestWorldFixture.CapturingPlayer player;
        private readonly NPC npc;
        private readonly List<CommunityGoal> goals = new();
        private readonly List<CommunityGoalRun> runs = new();
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public DonationTests()
        {
            fixture = new TestWorldFixture(s =>
            {
                s.GoldItemID = 700;
                s.ExperienceIconSheet = 5;
                s.ExperienceIconId = 6;
            });
            world = fixture.World;
            handler = world.CommunityGoals;
            var map = fixture.AddBaseMap(1, "m", 10, 10);
            player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            player.Spellbook = new Spellbook(player, fixture.Settings);
            map.AddPlayer(player, world);
            npc = new NPC
            {
                NPCTemplate = new NPCTemplate { NPCTemplateID = 7, Name = "Trainer" },
                NPCTemplateID = 7,
                Quests = new List<Quest>(),
                Map = map,
                MapID = map.ID,
                MapX = 5,
                MapY = 5,
            };
            map.AddNPC(npc);
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private const string ItemProps = """
            { "requirements": [ { "key": "logs", "type": "Item", "item": 900, "target": 1000 } ],
              "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private const string XpProps = """
            { "requirements": [ { "key": "exp", "type": "Experience", "target": 1000 } ],
              "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private const string LotteryProps = """
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "ranking": "lottery",
              "entryCost": { "gold": 1000 },
              "duration": "48h",
              "brackets": [ { "rank": 1, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private CommunityGoal LoadGoal(string properties, int id = 1, string? scriptPath = null)
        {
            var goal = new CommunityGoal
            {
                Id = id,
                Name = "Goal " + id,
                Description = "Desc",
                NpcTemplateId = 7,
                PropertiesJson = properties,
                ScriptPath = scriptPath ?? "",
            };
            goals.Add(goal);
            // LoadDefinitionRows replaces all definitions, so reload the accumulated goals each time.
            handler.LoadDefinitionRows(goals, world);
            return handler.Goals[id];
        }

        private CommunityGoalRun LoadRun(int goalId, RunStatus status, DateTime? completedAt,
                                         params (int Id, long Amount)[] contributors)
        {
            var run = new CommunityGoalRun
            {
                GoalId = goalId,
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
            runs.Add(run);
            // LoadStateRows replaces all state, so reload the accumulated runs each time.
            handler.LoadStateRows(runs, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return run;
        }

        private void RegisterItem(int id, string name, int sheet, int tile, int stackSize = 1) =>
            world.ItemHandler.AddTemplate(new ItemTemplate
            {
                ID = id, Name = name, Description = name, StackSize = stackSize,
                BaseStats = new AttributeSet(), ScriptParams = "",
                GraphicFile = sheet, GraphicTile = tile,
                GraphicR = 255, GraphicG = name == "Gold" ? 215 : 0, GraphicB = 0, GraphicA = 255,
            });

        private void GiveItems(int templateId, long count)
        {
            var template = world.ItemHandler.GetTemplate(templateId)!;
            var item = new Item();
            item.LoadFromTemplate(template);
            player.Inventory.SetSlot(1, new ItemSlot { Item = item, Stack = count });
        }

        private OptionListWindow OpenContributeWindow()
        {
            fixture.RunCommand(player, "LC5,5");
            var description = Assert.Single(player.Windows);
            fixture.RunCommand(player, $"WBC4,{description.ID},0,0,0");
            return Assert.IsType<OptionListWindow>(Assert.Single(player.Windows));
        }

        private AmountWindow OpenAmountWindow(OptionListWindow contribute)
        {
            fixture.RunCommand(player, $"WBC20,{contribute.ID},0,0,0");
            return Assert.IsType<AmountWindow>(Assert.Single(player.Windows));
        }

        [Fact]
        public void DonateItem_TakesItemsCreditsTotalAndReopensContribute()
        {
            LoadGoal(ItemProps);
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            var run = LoadRun(1, RunStatus.Active, null);
            GiveItems(900, 237);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            Assert.Contains(player.Sent, s => s.StartsWith($"AMI{amount.ID}|")
                && s.EndsWith("|Donate how many Log? You have 237, 1,000 still needed."));
            fixture.RunCommand(player, $"AMT{amount.ID},237");

            Assert.Equal(0, player.Inventory.GetInventoryItemFamilyCount(900));
            Assert.Equal(237, run.Totals["logs"]);
            Assert.Equal(237, run.Contributions[player.PlayerID].Amounts["logs"]);
            var reopened = Assert.IsType<OptionListWindow>(Assert.Single(player.Windows));
            Assert.StartsWith($"WNF{reopened.ID},1,Log 237 / 1,000 (you: 237)",
                player.Sent.Single(s => s.StartsWith($"WNF{reopened.ID},1,")));
        }

        [Fact]
        public void DonateItem_AdversarialOverGoalNeed_TakesOnlyRemaining()
        {
            LoadGoal(ItemProps);
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            var run = LoadRun(1, RunStatus.Active, null);
            run.Totals["logs"] = 950;
            GiveItems(900, 50);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            run.Totals["logs"] = 990;
            fixture.RunCommand(player, $"AMT{amount.ID},50");

            Assert.Equal(40, player.Inventory.GetInventoryItemFamilyCount(900));
            Assert.Equal(1000, run.Totals["logs"]);
            Assert.Equal(10, run.Contributions[player.PlayerID].Amounts["logs"]);
        }

        [Fact]
        public void DonateItem_ItemsDroppedBetweenOpenAndConfirm_TakesNothingCreditsNothing()
        {
            LoadGoal(ItemProps);
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            var run = LoadRun(1, RunStatus.Active, null);
            GiveItems(900, 237);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            player.Inventory.SetSlot(1, null);
            fixture.RunCommand(player, $"AMT{amount.ID},237");

            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
            Assert.Empty(player.Windows);
        }

        [Fact]
        public void DonateItem_RunCompletedBetweenOpenAndConfirm_SendsEndedMessage()
        {
            LoadGoal(ItemProps);
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            var run = LoadRun(1, RunStatus.Active, null);
            GiveItems(900, 237);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            run.Status = RunStatus.Completed;
            run.CompletedAt = now;
            fixture.RunCommand(player, $"AMT{amount.ID},237");

            Assert.Contains("NTF0,This goal has ended.\x1", player.Sent);
            Assert.Equal(237, player.Inventory.GetInventoryItemFamilyCount(900));
            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
        }

        [Fact]
        public void DonateItem_OutOfRangeAtConfirm_TakesNothingCreditsNothing()
        {
            LoadGoal(ItemProps);
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            var run = LoadRun(1, RunStatus.Active, null);
            GiveItems(900, 237);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            player.MapX = 60;
            fixture.RunCommand(player, $"AMT{amount.ID},237");

            Assert.Contains("NTF0,You are too far away.\x1", player.Sent);
            Assert.Equal(237, player.Inventory.GetInventoryItemFamilyCount(900));
            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
        }

        [Fact]
        public void DonateExperience_WithExperienceModifierTwo_TakesAndCreditsExactAmount()
        {
            LoadGoal(XpProps);
            var run = LoadRun(1, RunStatus.Active, null);
            player.Experience = 1000;
            world.Settings.ExperienceModifier = 2;

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            fixture.RunCommand(player, $"AMT{amount.ID},300");

            Assert.Equal(700, player.Experience);
            Assert.Equal(300, run.Totals["exp"]);
            Assert.Equal(300, run.Contributions[player.PlayerID].Amounts["exp"]);
        }

        [Fact]
        public void DonateExperience_AboveExperienceCap_TakesNothingCreditsNothing()
        {
            LoadGoal(XpProps);
            var run = LoadRun(1, RunStatus.Active, null);
            player.Experience = 1000;
            world.Settings.ExperienceCap = 500;

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            fixture.RunCommand(player, $"AMT{amount.ID},300");

            Assert.Equal(1000, player.Experience);
            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
        }

        [Fact]
        public void DonateItem_ScriptGateRefuses_TakesNothingCreditsNothingAndSendsRefusal()
        {
            fixture.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new() { Requirements = new[] { Requirement.Item(""logs"", 900, 1000) } };
    public override string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
        => ""Eligibility closed"";
}
return typeof(G);
");
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            LoadGoal("{}", scriptPath: "Scripts/CommunityGoal/G.csx");
            var run = LoadRun(1, RunStatus.Active, null);
            GiveItems(900, 237);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            fixture.RunCommand(player, $"AMT{amount.ID},237");

            Assert.Contains("NTF0,Eligibility closed\x1", player.Sent);
            Assert.Equal(237, player.Inventory.GetInventoryItemFamilyCount(900));
            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
            Assert.Empty(player.Windows);
        }

        [Fact]
        public void DonateItem_StatefulGateRefusesOnSecondCall_TakeEqualsCredit()
        {
            fixture.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    private int gateCalls;
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new() { Requirements = new[] { Requirement.Item(""logs"", 900, 1000) } };
    public override string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
        => this.gateCalls++ == 0 ? null : ""Second gate refused"";
}
return typeof(G);
");
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            LoadGoal("{}", scriptPath: "Scripts/CommunityGoal/G.csx");
            var run = LoadRun(1, RunStatus.Active, null);
            GiveItems(900, 237);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            fixture.RunCommand(player, $"AMT{amount.ID},237");

            Assert.Equal(0, player.Inventory.GetInventoryItemFamilyCount(900));
            Assert.Equal(237, run.Totals["logs"]);
            Assert.Equal(237, run.Contributions[player.PlayerID].Amounts["logs"]);
            Assert.DoesNotContain("NTF0,Second gate refused\x1", player.Sent);
        }

        [Fact]
        public void DonateItem_DonationCompletesGoal_ReopensHubInsteadOfStaleContributeWindow()
        {
            LoadGoal(ItemProps);
            RegisterItem(900, "Log", sheet: 3, tile: 4, stackSize: 1000);
            var run = LoadRun(1, RunStatus.Active, null);
            run.Totals["logs"] = 900;
            GiveItems(900, 100);

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            fixture.RunCommand(player, $"AMT{amount.ID},100");

            Assert.Equal(1000, run.Totals["logs"]);
            Assert.Equal(RunStatus.Completed, run.Status);
            var reopened = Assert.IsType<OptionListWindow>(Assert.Single(player.Windows));
            Assert.Contains(player.Sent, s => s.StartsWith($"WNF{reopened.ID},1,Claim: Goal 1 (Run 1)"));
            Assert.Contains(player.Sent, s => s.StartsWith($"WNF{reopened.ID},2,Goal 1 - Completed"));
        }

        [Fact]
        public void DonateLottery_TicketPurchase_TakesGoldCreditsEntries()
        {
            LoadGoal(LotteryProps);
            var run = LoadRun(1, RunStatus.Active, null);
            player.Gold = 5000;

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            Assert.Contains(player.Sent, s => s.StartsWith($"AMI{amount.ID}|")
                && s.EndsWith("|Buy how many tickets? (1,000g each)"));
            fixture.RunCommand(player, $"AMT{amount.ID},5");

            Assert.Equal(0, player.Gold);
            Assert.Equal(5000, run.Totals["gold"]);
            Assert.Equal(5, Ranking.Entries(handler.Goals[1].Definition!, run.Contributions[player.PlayerID]));
            var reopened = Assert.IsType<OptionListWindow>(Assert.Single(player.Windows));
            Assert.StartsWith($"WNF{reopened.ID},1,Tickets: 1,000g each - you hold 5 of 5",
                player.Sent.Single(s => s.StartsWith($"WNF{reopened.ID},1,")));
        }

        [Fact]
        public void DonateExperience_ReducesBankedXpWithoutChangingLevel()
        {
            LoadGoal(XpProps);
            var run = LoadRun(1, RunStatus.Active, null);
            player.Experience = 1000;
            var levelBefore = player.Level;

            var contribute = OpenContributeWindow();
            var amount = OpenAmountWindow(contribute);
            fixture.RunCommand(player, $"AMT{amount.ID},300");

            Assert.Equal(700, player.Experience);
            Assert.Equal(levelBefore, player.Level);
            Assert.Equal(300, run.Totals["exp"]);
            Assert.Equal(300, run.Contributions[player.PlayerID].Amounts["exp"]);
        }
    }
}
