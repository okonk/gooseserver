using System.Text.Json.Nodes;
using Goose.CommunityGoals;
using Goose.Quests;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.CommunityGoals
{
    public class CommunityRewardRegistryTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly NPC npc;
        private readonly TestWorldFixture.CapturingPlayer player;
        private readonly CommunityGoal goal;
        private readonly CommunityGoalRun run;

        public CommunityRewardRegistryTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            var map = fixture.AddBaseMap(1, "m", 60, 40);
            npc = new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Trainer" } };
            player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            player.Spellbook = new Spellbook(player, fixture.Settings);
            map.AddPlayer(player, world);
            goal = new CommunityGoal { Id = 1, Name = "Goal", NpcTemplateId = 0 };
            run = new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = DateTime.UtcNow };
        }

        public void Dispose() => fixture.Dispose();

        private static RewardSpec Spec(string type, JsonObject? params_ = null)
        {
            var obj = new JsonObject { ["type"] = type };
            if (params_ is not null)
                foreach (var (key, value) in params_)
                    obj[key] = value.DeepClone();
            return new RewardSpec { Type = type, Params = obj };
        }

        private CommunityRewardContext Ctx() => new()
        {
            Goal = goal, Run = run, Player = player, Npc = npc, World = world,
        };

        private Script<ICommunityRewardScript> CompileRewardScript(string body, string fileName = "R.csx")
        {
            Directory.CreateDirectory(Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal"));
            var relativePath = "Scripts/CommunityGoal/" + fileName;
            File.WriteAllText(Path.Combine(fixture.DataDirectory, relativePath), body);
            return world.ScriptHandler.GetScript<ICommunityRewardScript>(relativePath);
        }

        private void CopyShippedPotShare()
        {
            var dir = Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal");
            Directory.CreateDirectory(dir);
            File.Copy(Path.Combine(IllutiaDataDirectory(), "Scripts", "CommunityGoal", "PotShare.csx"),
                Path.Combine(dir, "PotShare.csx"));
        }

        private void RegisterItem(int id, string name, int stackSize = 1) =>
            world.ItemHandler.AddTemplate(new ItemTemplate
            {
                ID = id, Name = name, Description = name, StackSize = stackSize,
                BaseStats = new AttributeSet(), ScriptParams = "",
            });

        private static string IllutiaDataDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                foreach (var candidate in new[]
                {
                    Path.Combine(dir.FullName, "Data", "Illutia"),
                    Path.Combine(dir.FullName, "Goose", "Data", "Illutia"),
                })
                {
                    if (Directory.Exists(candidate)) return candidate;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate Data/Illutia from " + AppContext.BaseDirectory);
        }

        [Fact]
        public void Validate_personal_built_ins_are_valid()
        {
            Assert.Empty(CommunityRewardRegistry.Validate(Spec("Item", new JsonObject { ["value"] = 120, ["value2"] = 5 }), false));
            Assert.Empty(CommunityRewardRegistry.Validate(Spec("Gold", new JsonObject { ["value"] = 5000 }), false));
            Assert.Empty(CommunityRewardRegistry.Validate(Spec("Title", new JsonObject { ["value"] = "Master Builder" }), false));
        }

        [Fact]
        public void Validate_community_built_ins_are_valid()
        {
            Assert.Empty(CommunityRewardRegistry.Validate(Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = 1 }), true));
            Assert.Empty(CommunityRewardRegistry.Validate(Spec("WorldFlag", new JsonObject { ["name"] = "bridge_repaired" }), true));
        }

        [Fact]
        public void Validate_script_is_valid_in_both_scopes_with_a_script_path()
        {
            var spec = Spec("Script", new JsonObject
            {
                ["script"] = "Scripts/CommunityGoal/PotShare.csx",
                ["params"] = new JsonObject { ["key"] = "gold", ["percent"] = 90 },
            });

            Assert.Empty(CommunityRewardRegistry.Validate(spec, false));
            Assert.Empty(CommunityRewardRegistry.Validate(spec, true));
        }

        [Fact]
        public void Validate_unknown_type_is_rejected_in_both_scopes()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Banana", new JsonObject { ["value"] = 1 }), false),
                e => e.Contains("Banana"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Banana"), true),
                e => e.Contains("Banana"));
        }

        [Fact]
        public void Validate_personal_type_missing_value_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Gold"), false), e => e.Contains("value"));
        }

        [Fact]
        public void Validate_personal_type_with_non_scalar_value_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Gold", new JsonObject { ["value"] = new JsonObject() }), false),
                e => e.Contains("value"));
        }

        [Fact]
        public void Validate_fractional_value_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Gold", new JsonObject { ["value"] = 1.5 }), false),
                e => e.Contains("integer"));
        }

        [Fact]
        public void Validate_fractional_value2_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Item", new JsonObject { ["value"] = 900, ["value2"] = 2.5 }), false),
                e => e.Contains("value2"));
        }

        [Fact]
        public void Validate_string_value2_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Gold", new JsonObject { ["value"] = 5, ["value2"] = "2" }), false),
                e => e.Contains("value2"));
        }

        [Fact]
        public void Validate_numeric_value_for_a_string_type_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Title", new JsonObject { ["value"] = 120 }), false),
                e => e.Contains("string"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("HairColour", new JsonObject { ["value"] = 5 }), false),
                e => e.Contains("string"));
        }

        [Fact]
        public void Validate_string_value_for_a_numeric_type_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Gold", new JsonObject { ["value"] = "5" }), false),
                e => e.Contains("integer"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Item", new JsonObject { ["value"] = "900" }), false),
                e => e.Contains("integer"));
        }

        [Fact]
        public void Validate_world_flag_with_non_string_name_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("WorldFlag", new JsonObject { ["name"] = 5 }), true),
                e => e.Contains("name"));
        }

        [Fact]
        public void Validate_script_missing_script_path_is_rejected_in_both_scopes()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Script"), false), e => e.Contains("script"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Script"), true), e => e.Contains("script"));
        }

        [Fact]
        public void Validate_personal_only_type_as_community_reward_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Gold", new JsonObject { ["value"] = 100 }), true),
                e => e.Contains("Gold"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("Item", new JsonObject { ["value"] = 900 }), true),
                e => e.Contains("Item"));
        }

        [Fact]
        public void Validate_community_only_type_as_personal_reward_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = 1 }), false),
                e => e.Contains("ChestPages"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("WorldFlag", new JsonObject { ["name"] = "bridge_repaired" }), false),
                e => e.Contains("WorldFlag"));
        }

        [Fact]
        public void Validate_community_built_ins_missing_params_are_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("ChestPages", new JsonObject { ["npc"] = 900 }), true),
                e => e.Contains("pages"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("ChestPages", new JsonObject { ["pages"] = 1 }), true),
                e => e.Contains("npc"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("WorldFlag"), true),
                e => e.Contains("name"));
        }

        [Fact]
        public void Validate_chest_pages_zero_or_negative_pages_is_rejected()
        {
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = 0 }), true),
                e => e.Contains("pages"));
            Assert.Contains(CommunityRewardRegistry.Validate(Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = -2 }), true),
                e => e.Contains("pages"));
            Assert.Empty(CommunityRewardRegistry.Validate(Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = 1 }), true));
        }

        [Fact]
        public void Give_gold_grants_gold_with_the_quest_reward_message()
        {
            CommunityRewardRegistry.Give([Spec("Gold", new JsonObject { ["value"] = 500 })], Ctx());

            Assert.Equal(500, player.Gold);
            Assert.Contains(player.Sent, s => s.Contains("[Quest Reward]: 500 gold"));
        }

        [Fact]
        public void Give_item_grants_a_stacked_item_and_defaults_the_stack_to_one()
        {
            RegisterItem(900, "Sword", stackSize: 10);

            CommunityRewardRegistry.Give([Spec("Item", new JsonObject { ["value"] = 900, ["value2"] = 2 })], Ctx());

            var slot = Assert.Single(player.Inventory.GetInventorySlots(), s => s?.Item.Name == "Sword");
            Assert.Equal(2, slot!.Stack);
            Assert.Contains(player.Sent, s => s.Contains("[Quest Reward]: Item: Sword (2)"));

            CommunityRewardRegistry.Give([Spec("Item", new JsonObject { ["value"] = 900 })], Ctx());

            Assert.Equal(3, player.Inventory.GetInventorySlots().Single(s => s?.Item.Name == "Sword")!.Stack);
        }

        [Fact]
        public void Give_title_grants_the_title_with_the_quest_reward_message()
        {
            CommunityRewardRegistry.Give([Spec("Title", new JsonObject { ["value"] = "Master Builder" })], Ctx());

            Assert.Equal("Master Builder", player.Title);
            Assert.Contains(player.Sent, s => s.Contains("[Quest Reward]: Title: Master Builder"));
        }

        [Fact]
        public void Give_chest_pages_adds_to_the_chest_flag()
        {
            CommunityRewardRegistry.Give([Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = 2 })], Ctx());

            Assert.Equal(2, world.CommunityGoals.GetFlag("chest_pages:900"));
        }

        [Fact]
        public void Give_chest_pages_grants_accumulate_across_runs()
        {
            CommunityRewardRegistry.Give([Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = 1 })], Ctx());
            CommunityRewardRegistry.Give([Spec("ChestPages", new JsonObject { ["npc"] = 900, ["pages"] = 2 })], Ctx());

            Assert.Equal(3, world.CommunityGoals.GetFlag("chest_pages:900"));
        }

        [Fact]
        public void Give_world_flag_sets_the_flag()
        {
            CommunityRewardRegistry.Give([Spec("WorldFlag", new JsonObject { ["name"] = "bridge_repaired" })], Ctx());

            Assert.Equal(1, world.CommunityGoals.GetFlag("bridge_repaired"));
        }

        [Fact]
        public void RequiredSlots_counts_items_and_script_space()
        {
            CompileRewardScript(@"
public class R : BaseCommunityRewardScript
{
    public override int GetRequiredInventorySpace(CommunityRewardContext ctx) => 3;
}
return typeof(R);");
            RegisterItem(900, "Sword");

            var specs = new[]
            {
                Spec("Item", new JsonObject { ["value"] = 900 }),
                Spec("Gold", new JsonObject { ["value"] = 10 }),
                Spec("Script", new JsonObject { ["script"] = "Scripts/CommunityGoal/R.csx" }),
            };

            Assert.Equal(4, CommunityRewardRegistry.RequiredSlots(specs, Ctx()));
        }

        [Fact]
        public void RequiredSlots_ignores_scripts_that_reserve_nothing()
        {
            CompileRewardScript(@"
public class R : BaseCommunityRewardScript
{
}
return typeof(R);");

            Assert.Equal(0, CommunityRewardRegistry.RequiredSlots(
                [Spec("Script", new JsonObject { ["script"] = "Scripts/CommunityGoal/R.csx" })], Ctx()));
        }

        [Fact]
        public void CanGive_returns_the_script_refusal_and_null_for_built_ins()
        {
            CompileRewardScript(@"
public class R : BaseCommunityRewardScript
{
    public override string? CanGive(CommunityRewardContext ctx) => ""no room"";
}
return typeof(R);");

            Assert.Equal("no room", CommunityRewardRegistry.CanGive(
                [Spec("Script", new JsonObject { ["script"] = "Scripts/CommunityGoal/R.csx" })], Ctx()));
            Assert.Null(CommunityRewardRegistry.CanGive([Spec("Gold", new JsonObject { ["value"] = 1 })], Ctx()));
        }

        [Fact]
        public void PotShare_pays_the_share_of_the_configured_key()
        {
            CopyShippedPotShare();
            run.Totals["donations"] = 184000;

            var spec = Spec("Script", new JsonObject
            {
                ["script"] = "Scripts/CommunityGoal/PotShare.csx",
                ["params"] = new JsonObject { ["key"] = "donations", ["percent"] = 90 },
            });
            CommunityRewardRegistry.Give([spec], Ctx());

            Assert.Equal(165600, player.Gold);
            Assert.Contains(player.Sent, s => s.Contains("[Community Reward]"));
        }

        [Theory]
        [InlineData("""{ "percent": 90 }""")]
        [InlineData("""{ "key": "donations", "percent": 0 }""")]
        [InlineData("""{ "key": "donations", "percent": 101 }""")]
        [InlineData("""{ "key": "donations", "percent": 90.5 }""")]
        [InlineData("""{ "key": "donations", "percent": "90" }""")]
        public void PotShare_bad_params_give_nothing(string paramsJson)
        {
            CopyShippedPotShare();
            run.Totals["donations"] = 184000;

            var spec = Spec("Script", new JsonObject
            {
                ["script"] = "Scripts/CommunityGoal/PotShare.csx",
                ["params"] = (JsonObject)JsonNode.Parse(paramsJson)!,
            });
            CommunityRewardRegistry.Give([spec], Ctx());

            Assert.Equal(0, player.Gold);
        }

        [Fact]
        public void PotShare_pays_when_params_come_from_parsed_json()
        {
            CopyShippedPotShare();
            var def = CommunityGoalDefinitionParser.Parse("""
                {
                  "requirements": [ { "key": "donations", "type": "Gold", "target": 200000 } ],
                  "brackets": [
                    { "rank": 1, "rewards": [ { "type": "Script", "script": "Scripts/CommunityGoal/PotShare.csx", "params": { "key": "donations", "percent": 90 } } ] }
                  ]
                }
                """);
            run.Totals["donations"] = 184000;

            CommunityRewardRegistry.Give(def.Brackets[0].Rewards, Ctx());

            Assert.Equal(165600, player.Gold);
        }

        [Fact]
        public void PotShare_a_key_with_no_contributions_pays_nothing()
        {
            CopyShippedPotShare();
            run.Totals["gold"] = 184000;

            var spec = Spec("Script", new JsonObject
            {
                ["script"] = "Scripts/CommunityGoal/PotShare.csx",
                ["params"] = new JsonObject { ["key"] = "donations", ["percent"] = 90 },
            });
            CommunityRewardRegistry.Give([spec], Ctx());

            Assert.Equal(0, player.Gold);
        }
    }
}
