using Goose.Quests;
using Goose.Tests.Fakes;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

public class QuestScriptLoadingTests
{
    private static QuestScriptFixture FixtureWithValidScript()
    {
        var fixture = new QuestScriptFixture();
        try
        {
            fixture.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class Ok : BaseQuestScript { }
return typeof(Ok);
", "Ok.csx");
            return fixture;
        }
        catch
        {
            fixture.Dispose();
            throw;
        }
    }

    private static FakeDbDataReader RequirementRow(int type, string scriptPath) =>
        new(new Dictionary<string, object>
        {
            ["id"] = 42,
            ["requirement_type"] = type,
            ["requirement_value"] = 0L,
            ["requirement_value2"] = 0L,
            ["keep_requirement"] = "0",
            ["script_path"] = scriptPath,
            ["script_params"] = "{}",
        });

    private static FakeDbDataReader RewardRow(int type, string scriptPath) =>
        new(new Dictionary<string, object>
        {
            ["id"] = 43,
            ["reward_type"] = type,
            ["long_value"] = 0L,
            ["long_value2"] = 0L,
            ["string_value"] = "",
            ["script_path"] = scriptPath,
            ["script_params"] = "{}",
        });

    [Fact]
    public void A_script_requirement_without_a_script_path_fails_loading()
    {
        using var fixture = FixtureWithValidScript();
        var quest = new Quest { Id = 9 };

        var e = Assert.Throws<Exception>(() =>
            QuestRequirement.FromReader(RequirementRow((int)RequirementType.Script, ""), fixture.World, quest));

        // /reloadsql shows only e.Message to the GM (ReloadSQLCommandEvent.cs:40), so both ids
        // must be in the message or the GM cannot find the bad row.
        Assert.Contains("42", e.Message);
        Assert.Contains("9", e.Message);
    }

    [Fact]
    public void A_script_reward_without_a_script_path_fails_loading()
    {
        using var fixture = FixtureWithValidScript();
        var quest = new Quest { Id = 9 };

        var e = Assert.Throws<Exception>(() =>
            QuestReward.FromReader(RewardRow((int)RewardType.Script, ""), fixture.World, quest));

        Assert.Contains("43", e.Message);
        Assert.Contains("9", e.Message);
    }

    [Fact]
    public void A_script_path_naming_a_missing_file_fails_loading()
    {
        using var fixture = FixtureWithValidScript();
        Assert.ThrowsAny<Exception>(() => QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Nope.csx"), fixture.World, new Quest { Id = 9 }));
    }

    [Fact]
    public void A_script_requirement_with_a_script_loads()
    {
        using var fixture = FixtureWithValidScript();
        var req = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Ok.csx"), fixture.World, new Quest { Id = 9 });

        Assert.NotNull(req.Script);
        Assert.NotNull(req.Script.Object);
        Assert.Equal("{}", req.ScriptParams);
    }

    [Fact]
    public void A_non_script_requirement_without_a_script_loads_unchanged()
    {
        // The regression guard for every quest already in the shipped data.
        using var fixture = FixtureWithValidScript();
        var req = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Gold, ""), fixture.World, new Quest { Id = 9 });

        Assert.Equal(RequirementType.Gold, req.Type);
        Assert.Null(req.Script);
    }

    [Fact]
    public void A_script_reward_with_a_script_loads()
    {
        using var fixture = FixtureWithValidScript();
        var reward = QuestReward.FromReader(
            RewardRow((int)RewardType.Script, "Scripts/Quest/Ok.csx"), fixture.World, new Quest { Id = 9 });

        Assert.NotNull(reward.Script);
        Assert.NotNull(reward.Script.Object);
        Assert.Equal("{}", reward.ScriptParams);
    }

    [Fact]
    public void A_non_script_reward_without_a_script_loads_unchanged()
    {
        using var fixture = FixtureWithValidScript();
        var reward = QuestReward.FromReader(
            RewardRow((int)RewardType.Gold, ""), fixture.World, new Quest { Id = 9 });

        Assert.Equal(RewardType.Gold, reward.Type);
        Assert.Null(reward.Script);
    }

    [Fact]
    public void A_reward_path_naming_a_missing_file_fails_loading()
    {
        using var fixture = FixtureWithValidScript();
        Assert.ThrowsAny<Exception>(() => QuestReward.FromReader(
            RewardRow((int)RewardType.Script, "Scripts/Quest/Nope.csx"), fixture.World, new Quest { Id = 9 }));
    }

    [Fact]
    public void A_script_with_a_compile_error_fails_row_loading()
    {
        using var fixture = new QuestScriptFixture();
        File.WriteAllText(
            Path.Combine(fixture.DataDirectory, "Scripts", "Quest", "Broken.csx"),
            "this is not valid C#");

        Assert.ThrowsAny<Exception>(() => QuestReward.FromReader(
            RewardRow((int)RewardType.Script, "Scripts/Quest/Broken.csx"),
            fixture.World,
            new Quest { Id = 9 }));
    }

    [Fact]
    public void Server_and_editor_enum_values_stay_in_sync()
    {
        Assert.Equal(7, (int)RequirementType.Script);
        Assert.Equal(21, (int)RewardType.Script);
        Assert.Equal((int)CsvToSql.QuestRequirementsCsvToSql.RequirementType.Script,
                     (int)RequirementType.Script);
        Assert.Equal((int)CsvToSql.QuestRewardsCsvToSql.RewardType.Script,
                     (int)RewardType.Script);
    }

    [Fact]
    public void Two_rows_sharing_a_script_path_share_one_instance()
    {
        // Pins the shared-instance behaviour the interface doc warns about, so a future change
        // to per-row instances is a deliberate, visible decision.
        using var fixture = FixtureWithValidScript();
        var a = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Ok.csx"), fixture.World, new Quest { Id = 9 });
        var b = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Ok.csx"), fixture.World, new Quest { Id = 10 });

        Assert.Same(a.Script, b.Script);
    }
}
