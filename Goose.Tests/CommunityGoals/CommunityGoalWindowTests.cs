using System.Text.Json.Nodes;
using Goose;
using Goose.CommunityGoals;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.CommunityGoals
{
    public class CommunityGoalWindowTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly Map map;
        private readonly TestWorldFixture.CapturingPlayer player;
        private readonly NPC npc;
        private readonly List<CommunityGoal> goals = new();
        private readonly List<CommunityGoalRun> runs = new();
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public CommunityGoalWindowTests()
        {
            fixture = new TestWorldFixture(s =>
            {
                s.GoldItemID = 700;
                s.ExperienceIconSheet = 5;
                s.ExperienceIconId = 6;
            });
            world = fixture.World;
            handler = world.CommunityGoals;
            map = fixture.AddBaseMap(1, "m", 10, 10);
            player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            player.Spellbook = new Spellbook(player, fixture.Settings);
            map.AddPlayer(player, world);
            npc = new NPC
            {
                NPCTemplate = new NPCTemplate { NPCTemplateID = 7, Name = "Trainer" },
                NPCTemplateID = 7,
                Quests = new List<Quest>(),
                MapX = 5,
                MapY = 5,
            };
            map.AddNPC(npc);
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private const string ActiveProps = """
            { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
              "duration": "52h",
              "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private const string ClaimProps = """
            { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
              "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private const string ContributeProps = """
            { "requirements": [
                { "key": "sword", "type": "Item", "item": 900, "target": 10 },
                { "key": "gold", "type": "Gold", "target": 100 },
                { "key": "exp", "type": "Experience", "target": 50 },
                { "key": "kills", "type": "Kill", "npc": 8, "target": 5 },
                { "key": "custom", "type": "Custom", "label": "Magic", "target": 4 } ],
              "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private const string LotteryProps = """
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "ranking": "lottery",
              "entryCost": { "gold": 5 },
              "duration": "48h",
              "brackets": [ { "rank": 1, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private const string KillProps = """
            { "requirements": [ { "key": "kills", "type": "Kill", "npc": 8, "target": 5 } ],
              "brackets": [ { "minScore": 0, "rewards": [ { "type": "Gold", "value": 10 } ] } ] }
            """;

        private const string ItemRewardProps = """
            { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
              "brackets": [ { "minScore": 0, "rewards": [ { "type": "Item", "value": 900, "value2": 1 } ] } ] }
            """;

        private CommunityGoal LoadGoal(string properties, string description = "Desc", int id = 1)
        {
            var goal = new CommunityGoal
            {
                Id = id,
                Name = "Goal " + id,
                Description = description,
                NpcTemplateId = 7,
                PropertiesJson = properties,
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

        private void RegisterItem(int id, string name, int sheet, int tile) =>
            world.ItemHandler.AddTemplate(new ItemTemplate
            {
                ID = id, Name = name, Description = name, StackSize = 1,
                BaseStats = new AttributeSet(), ScriptParams = "",
                GraphicFile = sheet, GraphicTile = tile,
                GraphicR = 255, GraphicG = name == "Gold" ? 215 : 0, GraphicB = 0, GraphicA = 255,
            });

        private List<string> TextLines(int windowId)
            => player.Sent.Where(s => s.StartsWith($"WNF{windowId},"))
                .Select(s =>
                {
                    var rest = s.Substring($"WNF{windowId},".Length);
                    return rest[(rest.IndexOf(',') + 1)..].Split('|')[0];
                })
                .ToList();

        private void FillInventory()
        {
            RegisterItem(950, "Filler", sheet: 0, tile: 0);
            var filler = world.ItemHandler.GetTemplate(950)!;
            for (var i = 1; i <= fixture.Settings.InventorySize; i++)
            {
                var item = new Item();
                item.LoadFromTemplate(filler);
                player.Inventory.SetSlot(i, new ItemSlot { Item = item });
            }
        }

        [Fact]
        public void LeftClick_GoalNpc_OpensHubNotQuestWindow()
        {
            LoadGoal(ActiveProps, id: 1);
            LoadGoal(ActiveProps, id: 2);
            LoadRun(1, RunStatus.Active, null);
            LoadRun(2, RunStatus.Active, null);
            npc.Quests = new List<Quest> { new() { Id = 1, Name = "Q", Description = "d", FailText = "f", PassText = "p" } };

            fixture.RunCommand(player, "LC5,5");

            var window = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.OptionList, window.Type);
            Assert.Empty(player.QuestsStarted);
            Assert.Contains(player.Sent, s => s.StartsWith($"WNL{window.ID},Community Goals"));
            Assert.Equal(new[] { $"WNF{window.ID},1,Goal 1 - 0% - 2d 4h|0|0|0|0|*\x1", $"WNF{window.ID},2,Goal 2 - 0% - 2d 4h|0|0|0|0|*\x1" },
                player.Sent.Where(s => s.StartsWith($"WNF{window.ID},")).ToArray());
            Assert.All(TextLines(window.ID), line => Assert.All(line, c => Assert.True(c < 128, $"non-ASCII char {c} in hub line")));
        }

        [Fact]
        public void LeftClick_GoalNpcWithNothingToShow_OpensNoWindow()
        {
            LoadGoal(ClaimProps);

            fixture.RunCommand(player, "LC5,5");

            Assert.Empty(player.Windows);
        }

        [Fact]
        public void LeftClick_SingleActiveGoal_SkipsHubAndOpensDescription()
        {
            LoadGoal(ActiveProps);
            LoadRun(1, RunStatus.Active, null);

            fixture.RunCommand(player, "LC5,5");

            var window = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.CommunityGoal, window.Type);
            Assert.Equal(Window.WindowFrames.Quest, window.Frame);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("WNL"));
        }

        [Fact]
        public void Description_WrapsLongTextToAtMostFiftyChars()
        {
            LoadGoal(ActiveProps, description: new string('a', 130));
            LoadRun(1, RunStatus.Active, null);

            fixture.RunCommand(player, "LC5,5");

            var window = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.CommunityGoal, window.Type);
            var lines = TextLines(window.ID);
            Assert.All(lines, line => Assert.True(line.Length <= 50, $"line too long: '{line}'"));
            Assert.Contains("", lines);
        }

        [Fact]
        public void Contribute_ShowsTotalsAndIcons()
        {
            LoadGoal(ContributeProps);
            RegisterItem(900, "Sword", sheet: 3, tile: 9);
            RegisterItem(700, "Gold", sheet: 1, tile: 2);
            world.NPCHandler.AddTemplate(new NPCTemplate
            {
                NPCTemplateID = 8, Name = "Slay", BaseStats = new AttributeSet(),
            });
            var run = LoadRun(1, RunStatus.Active, null, (player.PlayerID, 20));
            run.Totals["sword"] = 3;
            run.Totals["gold"] = 50;
            run.Totals["exp"] = 10;
            run.Totals["kills"] = 2;
            run.Totals["custom"] = 1;
            var mine = run.Contributions[player.PlayerID];
            mine.Amounts["sword"] = 1;
            mine.Amounts["exp"] = 4;

            fixture.RunCommand(player, "LC5,5");
            var description = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.CommunityGoal, description.Type);
            fixture.RunCommand(player, $"WBC4,{description.ID},0,0,0");

            var contribute = Assert.Single(player.Windows);
            Assert.IsType<OptionListWindow>(contribute);
            Assert.Equal(
                new[]
                {
                    $"WNF{contribute.ID},1,Sword 3 / 10 (you: 1)|0|0|3|9|255|0|0|255\x1",
                    $"WNF{contribute.ID},2,Gold 50 / 100 (you: 20)|0|0|1|2|255|215|0|255\x1",
                    $"WNF{contribute.ID},3,Experience 10 / 50 (you: 4)|0|0|5|6|*\x1",
                    $"WNF{contribute.ID},4,Slay 2 / 5 (you: 0)|0|0|0|0|*\x1",
                    $"WNF{contribute.ID},5,Magic 1 / 4|0|0|0|0|*\x1",
                    $"WNF{contribute.ID},6,Top contributors|0|0|0|0|*\x1",
                },
                player.Sent.Where(s => s.StartsWith($"WNF{contribute.ID},")).ToArray());
        }

        private void LoadScaledRuns()
        {
            runs.Add(new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = started, Status = RunStatus.Completed, CompletedAt = started });
            var active = new CommunityGoalRun { GoalId = 1, Run = 2, StartedAt = started };
            active.Totals["gold"] = 30;
            runs.Add(active);
            handler.LoadStateRows(runs, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
        }

        [Theory]
        [InlineData(""", "maxCompletions": 5""", "Upgrade 2 of 5")]
        [InlineData("", "Upgrade 2")]
        public void Description_RepeatingGoal_ShowsUpgradeLine(string extra, string expected)
        {
            LoadGoal("""{ "requirements": [ { "key": "gold", "type": "Gold", "target": 100, "growth": 2 } ], "repeat": "onClose" """ + extra + " }");
            LoadScaledRuns();

            fixture.RunCommand(player, "LC5,5");

            var window = Assert.Single(player.Windows);
            Assert.Equal(new[] { expected, "Desc", "", "Progress: 15%" }, TextLines(window.ID).ToArray());
        }

        [Fact]
        public void Description_OneRunGoal_HasNoUpgradeLine()
        {
            LoadGoal(ClaimProps);
            LoadRun(1, RunStatus.Active, null);

            fixture.RunCommand(player, "LC5,5");

            var window = Assert.Single(player.Windows);
            Assert.Equal("Desc", TextLines(window.ID)[0]);
        }

        [Fact]
        public void Contribute_LaterRun_ShowsScaledTarget()
        {
            LoadGoal("""{ "requirements": [ { "key": "gold", "type": "Gold", "target": 100, "growth": 2 } ], "repeat": "onClose" }""");
            RegisterItem(700, "Gold", sheet: 1, tile: 2);
            LoadScaledRuns();

            fixture.RunCommand(player, "LC5,5");
            var description = Assert.Single(player.Windows);
            fixture.RunCommand(player, $"WBC4,{description.ID},0,0,0");

            var contribute = Assert.Single(player.Windows);
            Assert.Contains($"WNF{contribute.ID},1,Gold 30 / 200 (you: 0)|0|0|1|2|255|215|0|255\x1", player.Sent);
        }

        [Fact]
        public void Hub_ListsOnlyFinishedRunsCompletedWithinFourteenDays()
        {
            LoadGoal(ClaimProps, id: 1);
            LoadGoal(ClaimProps, id: 2);
            LoadRun(1, RunStatus.Completed, now - (TimeSpan.FromDays(14) - TimeSpan.FromMinutes(1)));
            LoadRun(2, RunStatus.Completed, now - (TimeSpan.FromDays(14) + TimeSpan.FromMinutes(1)));

            fixture.RunCommand(player, "LC5,5");

            var window = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.OptionList, window.Type);
            Assert.Equal(new[] { "Goal 1 - Completed" }, TextLines(window.ID).ToArray());
        }

        [Fact]
        public void PrizeClaim_SuccessRemovesLineOnReopen()
        {
            LoadGoal(ClaimProps);
            LoadRun(1, RunStatus.Completed, started, (player.PlayerID, 50));

            fixture.RunCommand(player, "LC5,5");
            var hub = Assert.Single(player.Windows);
            Assert.Equal(new[] { "Claim: Goal 1 (Run 1)", "Goal 1 - Completed" }, TextLines(hub.ID).ToArray());

            fixture.RunCommand(player, $"WBC20,{hub.ID},0,0,0");

            var reopened = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.OptionList, reopened.Type);
            Assert.Equal(new[] { "Goal 1 - Completed" }, TextLines(reopened.ID).ToArray());
            Assert.Equal(10, player.Gold);
        }

        [Fact]
        public void Contribute_LotteryGoal_ShowsTicketLine()
        {
            LoadGoal(LotteryProps);
            LoadRun(1, RunStatus.Active, null, (player.PlayerID, 15), (42, 42));

            fixture.RunCommand(player, "LC5,5");
            var description = Assert.Single(player.Windows);
            fixture.RunCommand(player, $"WBC4,{description.ID},0,0,0");

            var contribute = Assert.Single(player.Windows);
            Assert.IsType<OptionListWindow>(contribute);
            Assert.Equal(new[]
            {
                $"WNF{contribute.ID},1,Tickets: 5g each - you hold 3 of 11|0|0|0|0|*\x1",
                $"WNF{contribute.ID},2,Top contributors|0|0|0|0|*\x1",
            },
                player.Sent.Where(s => s.StartsWith($"WNF{contribute.ID},")).ToArray());
        }

        [Fact]
        public void TopContributors_ActiveRun_ListsTopTenAndBackReturnsToContribute()
        {
            LoadGoal(ActiveProps);
            var contributors = new List<(int Id, long Amount)> { (player.PlayerID, 100) };
            for (var i = 1; i <= 11; i++)
                contributors.Add((100 + i, 100L * i));
            LoadRun(1, RunStatus.Active, null, contributors.ToArray());

            fixture.RunCommand(player, "LC5,5");
            var description = Assert.Single(player.Windows);
            fixture.RunCommand(player, $"WBC4,{description.ID},0,0,0");
            var contribute = Assert.Single(player.Windows);
            fixture.RunCommand(player, $"WBC21,{contribute.ID},0,0,0");

            var top = Assert.Single(player.Windows);
            Assert.IsType<OptionListWindow>(top);
            var expected = Enumerable.Range(1, 8).Select(i => $"{i}. Unknown - {(12 - i) * 100:N0}").ToArray();
            Assert.Equal(expected, TextLines(top.ID).ToArray());

            var before = player.Sent.Count;
            fixture.RunCommand(player, $"WBC4,{top.ID},0,0,0");
            Assert.Equal(new[]
            {
                $"WNF{top.ID},1,9. Unknown - 300|0|0|0|0|*\x1",
                $"WNF{top.ID},2,10. Unknown - 200|0|0|0|0|*\x1",
            },
                player.Sent.Skip(before).Where(s => s.StartsWith($"WNF{top.ID},")).ToArray());

            fixture.RunCommand(player, $"WBC3,{top.ID},0,0,0");
            fixture.RunCommand(player, $"WBC3,{top.ID},0,0,0");

            var back = Assert.Single(player.Windows);
            Assert.IsType<OptionListWindow>(back);
            Assert.StartsWith($"WNF{back.ID},1,Gold 6,700 / 100 (you: 100)",
                player.Sent.Single(s => s.StartsWith($"WNF{back.ID},1,")));
        }

        [Fact]
        public void Contribute_KillLineClick_SendsSlayMessage()
        {
            LoadGoal(KillProps);
            world.NPCHandler.AddTemplate(new NPCTemplate
            {
                NPCTemplateID = 8, Name = "Slay", BaseStats = new AttributeSet(),
            });
            LoadRun(1, RunStatus.Active, null);

            fixture.RunCommand(player, "LC5,5");
            var description = Assert.Single(player.Windows);
            fixture.RunCommand(player, $"WBC4,{description.ID},0,0,0");
            var contribute = Assert.Single(player.Windows);
            fixture.RunCommand(player, $"WBC20,{contribute.ID},0,0,0");

            Assert.Contains("NTF0,Slay Slay to contribute.\x1", player.Sent);
            Assert.Empty(player.Windows);
        }

        [Fact]
        public void FinishedDescription_ShowsCompletedDateAndPlaces()
        {
            LoadGoal(ClaimProps);
            var run = LoadRun(1, RunStatus.Completed, started, (player.PlayerID, 50), (42, 30));
            run.Results = new JsonObject
            {
                ["places"] = new JsonArray(player.PlayerID, 42),
                ["top"] = new JsonArray(),
            };
            fixture.RegisterDatabasePlayer(player);

            fixture.RunCommand(player, "LC5,5");
            var hub = Assert.Single(player.Windows);
            Assert.IsType<OptionListWindow>(hub);
            fixture.RunCommand(player, $"WBC21,{hub.ID},0,0,0");

            var window = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.CommunityGoal, window.Type);
            Assert.Equal(new[] { "Desc", "", "Completed 2025-06-01", "1st: Tester", "2nd: Unknown" },
                TextLines(window.ID).ToArray());
        }

        [Fact]
        public void FailedDescription_ShowsFailedDate()
        {
            LoadGoal(ClaimProps);
            LoadRun(1, RunStatus.Failed, started);

            fixture.RunCommand(player, "LC5,5");
            var hub = Assert.Single(player.Windows);
            Assert.IsType<OptionListWindow>(hub);
            fixture.RunCommand(player, $"WBC20,{hub.ID},0,0,0");

            var window = Assert.Single(player.Windows);
            Assert.Equal(Window.WindowTypes.CommunityGoal, window.Type);
            Assert.Equal(new[] { "Desc", "", "Failed 2025-06-01" }, TextLines(window.ID).ToArray());
        }

        [Fact]
        public void PrizeClaim_RefusalSendsNoticeAndClosesHub()
        {
            LoadGoal(ItemRewardProps);
            RegisterItem(900, "Sword", sheet: 3, tile: 9);
            LoadRun(1, RunStatus.Completed, started, (player.PlayerID, 50));
            FillInventory();

            fixture.RunCommand(player, "LC5,5");
            var hub = Assert.Single(player.Windows);
            Assert.Equal(new[] { "Claim: Goal 1 (Run 1)", "Goal 1 - Completed" }, TextLines(hub.ID).ToArray());
            fixture.RunCommand(player, $"WBC20,{hub.ID},0,0,0");

            Assert.Contains("NTF0,You need 1 free inventory slots to claim this.\x1", player.Sent);
            Assert.Empty(player.Windows);
        }
    }
}
