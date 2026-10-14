using Goose.CommunityGoals;

namespace Goose.Tests;

public class CommunityGoalDefinitionParserTests
{
    private const string MilestoneJson = """
        {
          "requirements": [
            { "key": "logs", "type": "Item", "item": 120, "target": 5000 },
            { "key": "gold", "type": "Gold", "target": 1000000 },
            { "key": "orcs", "type": "Kill", "npc": 44, "target": 10000 }
          ],
          "score": { "logs": 1, "gold": 0.01, "orcs": 5 },
          "brackets": [
            { "rank": 1, "rewards": [ { "type": "Title", "value": "Master Builder" } ] },
            { "minScore": 2000, "rewards": [ { "type": "Gold", "value": 5000 } ] },
            { "minScore": 1, "rewards": [ { "type": "Gold", "value": 100 } ] }
          ],
          "communityRewards": [
            { "type": "ChestPages", "npc": 900, "pages": 1 },
            { "type": "WorldFlag", "name": "bridge_repaired" }
          ]
        }
        """;

    private const string LotteryJson = """
        {
          "requirements": [ { "key": "gold", "type": "Gold" } ],
          "ranking": "lottery",
          "entryCost": { "gold": 1000 },
          "duration": "7d",
          "repeat": "onClose",
          "brackets": [
            { "rank": 1, "rewards": [ { "type": "Script", "script": "CommunityGoal/PotShare.csx", "params": { "key": "gold", "percent": 90 } } ] },
            { "rank": 2, "rewards": [ { "type": "Script", "script": "CommunityGoal/PotShare.csx", "params": { "key": "gold", "percent": 10 } } ] }
          ]
        }
        """;

    private static IReadOnlyList<string> Validate(string json, bool customScript = false)
        => CommunityGoalDefinitionParser.Validate(CommunityGoalDefinitionParser.Parse(json), customScript);

    private static IReadOnlyList<string> Validate(CommunityGoalDefinition def, bool customScript = false)
        => CommunityGoalDefinitionParser.Validate(def, customScript);

    [Fact]
    public void Milestone_example_parses_every_field()
    {
        var def = CommunityGoalDefinitionParser.Parse(MilestoneJson);

        Assert.Equal(3, def.Requirements.Count);
        var logs = def.Requirements[0];
        Assert.Equal("logs", logs.Key);
        Assert.Equal(RequirementKind.Item, logs.Kind);
        Assert.Equal(120, logs.ItemTemplateId);
        Assert.Equal(5000, logs.Target);
        var gold = def.Requirements[1];
        Assert.Equal("gold", gold.Key);
        Assert.Equal(RequirementKind.Gold, gold.Kind);
        Assert.Null(gold.ItemTemplateId);
        Assert.Equal(1000000, gold.Target);
        var orcs = def.Requirements[2];
        Assert.Equal("orcs", orcs.Key);
        Assert.Equal(RequirementKind.Kill, orcs.Kind);
        Assert.Equal(44, orcs.NpcTemplateId);
        Assert.Equal(10000, orcs.Target);

        Assert.Equal(1.0, def.ScoreWeights["logs"]);
        Assert.Equal(0.01, def.ScoreWeights["gold"]);
        Assert.Equal(5.0, def.ScoreWeights["orcs"]);
        Assert.Equal(RankingKind.Score, def.Ranking);
        Assert.Null(def.EntryCost);
        Assert.Null(def.Duration);
        Assert.Null(def.ClosesAt);
        Assert.Null(def.Repeat);
        Assert.Null(def.ClaimWindow);
        Assert.True(def.HasTargets);
        Assert.False(def.Repeats);
        Assert.Null(def.EffectiveClaimWindow);

        Assert.Equal(3, def.Brackets.Count);
        var rank1 = def.Brackets[0];
        Assert.Equal(1, rank1.Rank);
        Assert.Null(rank1.MinScore);
        Assert.Null(rank1.Measure);
        Assert.Equal("Title", rank1.Rewards[0].Type);
        Assert.Equal("Master Builder", rank1.Rewards[0].Params["value"]!.GetValue<string>());
        Assert.Equal(2000, def.Brackets[1].MinScore);
        Assert.Equal(1, def.Brackets[2].MinScore);

        Assert.Equal(2, def.CommunityRewards.Count);
        Assert.Equal("ChestPages", def.CommunityRewards[0].Type);
        Assert.Equal(900, def.CommunityRewards[0].Params["npc"]!.GetValue<int>());
        Assert.Equal(1, def.CommunityRewards[0].Params["pages"]!.GetValue<int>());
        Assert.Equal("WorldFlag", def.CommunityRewards[1].Type);
        Assert.Equal("bridge_repaired", def.CommunityRewards[1].Params["name"]!.GetValue<string>());

        Assert.Empty(Validate(MilestoneJson));
    }

    [Fact]
    public void Lottery_example_parses_every_field()
    {
        var def = CommunityGoalDefinitionParser.Parse(LotteryJson);

        Assert.Single(def.Requirements);
        var gold = def.Requirements[0];
        Assert.Equal("gold", gold.Key);
        Assert.Equal(RequirementKind.Gold, gold.Kind);
        Assert.Null(gold.Target);
        Assert.False(def.HasTargets);

        Assert.Equal(1.0, def.ScoreWeights["gold"]);
        Assert.Equal(RankingKind.Lottery, def.Ranking);
        Assert.Equal(1000, def.EntryCost!["gold"]);
        Assert.Equal(TimeSpan.FromDays(7), def.Duration);
        Assert.NotNull(def.Repeat);
        Assert.Null(def.Repeat!.Every);
        Assert.True(def.Repeats);
        Assert.Equal(TimeSpan.FromDays(30), def.EffectiveClaimWindow);

        Assert.Equal(2, def.Brackets.Count);
        Assert.Equal(1, def.Brackets[0].Rank);
        Assert.Equal(2, def.Brackets[1].Rank);
        var first = def.Brackets[0].Rewards[0];
        Assert.Equal("Script", first.Type);
        Assert.Equal("CommunityGoal/PotShare.csx", first.Params["script"]!.GetValue<string>());
        Assert.Equal("gold", first.Params["params"]!["key"]!.GetValue<string>());
        Assert.Equal(90, first.Params["params"]!["percent"]!.GetValue<int>());
        Assert.Equal(10, def.Brackets[1].Rewards[0].Params["params"]!["percent"]!.GetValue<int>());

        Assert.Empty(def.CommunityRewards);

        Assert.Empty(Validate(LotteryJson));
    }

    [Fact]
    public void ClosesAt_repeat_every_and_claim_window_parse()
    {
        var def = CommunityGoalDefinitionParser.Parse("""
            {
              "requirements": [ { "key": "gold", "type": "Gold", "target": 10 } ],
              "closesAt": "2026-11-01T12:00:00Z",
              "repeat": { "every": "12h" },
              "claimWindow": "30m"
            }
            """);

        Assert.Equal(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc), def.ClosesAt);
        Assert.Equal(TimeSpan.FromHours(12), def.Repeat!.Every);
        Assert.Equal(TimeSpan.FromMinutes(30), def.ClaimWindow);
        Assert.Equal(TimeSpan.FromMinutes(30), def.EffectiveClaimWindow);
        Assert.Empty(Validate(def, false));
    }

    [Theory]
    [InlineData("7d", 7, 0, 0)]
    [InlineData("12h", 0, 12, 0)]
    [InlineData("30m", 0, 0, 30)]
    public void Duration_accepts_d_h_m(string duration, int days, int hours, int minutes)
    {
        var def = CommunityGoalDefinitionParser.Parse(
            $"{{ \"requirements\": [ {{ \"key\": \"gold\", \"type\": \"Gold\" }} ], \"duration\": \"{duration}\" }}");
        Assert.Equal(new TimeSpan(days, hours, minutes, 0), def.Duration);
    }

    [Fact]
    public void Duplicate_property_in_nested_object_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "gold", "type": "Gold" } ],
                  "score": { "gold": 1, "gold": 2 } }
                """));
        Assert.Contains("duplicate property 'gold'", ex.Message);
    }

    [Fact]
    public void Duplicate_root_property_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [], "requirements": [] }
                """));
        Assert.Contains("duplicate property 'requirements'", ex.Message);
    }

    [Fact]
    public void Same_property_name_in_different_objects_is_valid()
    {
        var def = CommunityGoalDefinitionParser.Parse("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "duration": "1d",
              "brackets": [ { "rank": 1, "rewards": [ { "type": "Gold", "value": 100 } ] } ],
              "communityRewards": [ { "type": "Gold", "value": 10 } ] }
            """);
        Assert.Empty(Validate(def, false));
    }

    [Fact]
    public void Malformed_json_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() => CommunityGoalDefinitionParser.Parse("not json"));
        Assert.Contains("invalid JSON", ex.Message);
    }

    [Fact]
    public void Root_not_object_throws()
    {
        Assert.Throws<CommunityGoalDefinitionException>(() => CommunityGoalDefinitionParser.Parse("[1, 2]"));
    }

    [Fact]
    public void Unknown_requirement_type_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "x", "type": "Fishing", "target": 10 } ] }
                """));
        Assert.Contains("unknown requirement type 'Fishing'", ex.Message);
    }

    [Fact]
    public void Missing_requirement_key_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "type": "Gold", "target": 10 } ] }
                """));
        Assert.Contains("key", ex.Message);
    }

    [Fact]
    public void Duplicate_requirement_key_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "gold", "type": "Gold" }, { "key": "gold", "type": "Gold" } ] }
                """));
        Assert.Contains("duplicate requirement key 'gold'", ex.Message);
    }

    [Theory]
    [InlineData("7x")]
    [InlineData("0d")]
    [InlineData("-1h")]
    [InlineData("1.5d")]
    [InlineData("d")]
    [InlineData("")]
    public void Malformed_duration_throws(string duration)
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse(
                $"{{ \"requirements\": [ {{ \"key\": \"gold\", \"type\": \"Gold\" }} ], \"duration\": \"{duration}\" }}"));
        Assert.Contains($"invalid duration '{duration}'", ex.Message);
    }

    [Fact]
    public void Malformed_closesAt_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "gold", "type": "Gold" } ], "closesAt": "yesterday" }
                """));
        Assert.Contains("invalid closesAt 'yesterday'", ex.Message);
    }

    [Fact]
    public void Reward_without_type_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "gold", "type": "Gold" } ],
                  "brackets": [ { "rank": 1, "rewards": [ { "value": 100 } ] } ] }
                """));
        Assert.Contains("type", ex.Message);
    }

    [Fact]
    public void Reward_not_object_throws()
    {
        Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "gold", "type": "Gold" } ],
                  "communityRewards": [ 42 ] }
                """));
    }

    [Fact]
    public void Unknown_ranking_throws()
    {
        var ex = Assert.Throws<CommunityGoalDefinitionException>(() =>
            CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "gold", "type": "Gold" } ], "ranking": "dice", "duration": "1d" }
                """));
        Assert.Contains("unknown ranking 'dice'", ex.Message);
    }

    [Fact]
    public void Validate_rejects_item_without_item()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "logs", "type": "Item" } ], "duration": "1d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("item", error);
    }

    [Fact]
    public void Validate_rejects_kill_without_npc()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "orcs", "type": "Kill" } ], "duration": "1d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("npc", error);
    }

    [Fact]
    public void Validate_rejects_score_referencing_missing_key()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "score": { "nope": 1 }, "duration": "1d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("'nope'", error);
    }

    [Fact]
    public void Validate_rejects_bracket_measure_referencing_missing_key()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "brackets": [ { "minScore": 1, "measure": "nope", "rewards": [] } ], "duration": "1d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("'nope'", error);
    }

    [Fact]
    public void Validate_rejects_bracket_with_neither_rank_nor_minScore()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "brackets": [ { "rewards": [] } ], "duration": "1d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("rank", error);
    }

    [Fact]
    public void Validate_rejects_bracket_with_both_rank_and_minScore()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "brackets": [ { "rank": 1, "minScore": 5, "rewards": [] } ], "duration": "1d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("rank", error);
    }

    [Fact]
    public void Validate_rejects_lottery_without_entry_cost()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "ranking": "lottery", "duration": "7d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("entryCost", error);
    }

    [Fact]
    public void Validate_rejects_entry_cost_key_not_a_requirement()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ],
              "ranking": "lottery", "entryCost": { "nope": 1 }, "duration": "7d" }
            """);
        var error = Assert.Single(errors);
        Assert.Contains("'nope'", error);
    }

    [Fact]
    public void Validate_rejects_targetless_goal_with_no_termination()
    {
        var errors = Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ] }
            """);
        Assert.Single(errors);
    }

    [Fact]
    public void Targetless_goal_with_no_termination_is_valid_with_custom_script()
    {
        Assert.Empty(Validate("""
            { "requirements": [ { "key": "gold", "type": "Gold" } ] }
            """, customScript: true));
    }

    [Fact]
    public void Factories_build_definitions()
    {
        var rewards = new[] { new RewardSpec { Type = "Gold", Params = new() } };
        var def = new CommunityGoalDefinition
        {
            Requirements =
            [
                Requirement.Item("logs", 120, 5000),
                Requirement.Gold("gold"),
                Requirement.Experience("xp", 900000),
                Requirement.Kill("orcs", 44),
                Requirement.Custom("boss_damage", "Boss damage")
            ],
            Brackets =
            [
                Bracket.Rank(1, rewards),
                Bracket.MinScore(2000, rewards, "logs")
            ],
            Repeat = Repeat.Every(TimeSpan.FromDays(7)),
            ClaimWindow = TimeSpan.FromDays(2)
        };

        Assert.Equal(RequirementKind.Item, def.Requirements[0].Kind);
        Assert.Equal(120, def.Requirements[0].ItemTemplateId);
        Assert.Equal(5000, def.Requirements[0].Target);
        Assert.Equal(RequirementKind.Gold, def.Requirements[1].Kind);
        Assert.Null(def.Requirements[1].Target);
        Assert.Equal(RequirementKind.Experience, def.Requirements[2].Kind);
        Assert.Equal(900000, def.Requirements[2].Target);
        Assert.Equal(RequirementKind.Kill, def.Requirements[3].Kind);
        Assert.Equal(44, def.Requirements[3].NpcTemplateId);
        Assert.Equal(RequirementKind.Custom, def.Requirements[4].Kind);
        Assert.Equal("Boss damage", def.Requirements[4].Label);

        Assert.Equal(1, def.Brackets[0].Rank);
        Assert.Same(rewards, def.Brackets[0].Rewards);
        Assert.Equal(2000, def.Brackets[1].MinScore);
        Assert.Equal("logs", def.Brackets[1].Measure);

        Assert.True(def.Repeats);
        Assert.Equal(TimeSpan.FromDays(7), def.Repeat!.Every);
        Assert.Equal(TimeSpan.FromDays(2), def.EffectiveClaimWindow);

        Assert.Null(Repeat.OnClose.Every);
    }

    [Fact]
    public void Effective_claim_window_is_null_when_not_repeating()
    {
        var def = CommunityGoalDefinitionParser.Parse("""
            { "requirements": [ { "key": "gold", "type": "Gold", "target": 10 } ], "claimWindow": "30m" }
            """);
        Assert.Equal(TimeSpan.FromMinutes(30), def.ClaimWindow);
        Assert.Null(def.EffectiveClaimWindow);
    }
}
