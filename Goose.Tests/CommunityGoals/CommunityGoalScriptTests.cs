using Goose.CommunityGoals;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class CommunityGoalScriptTests : IDisposable
{
    private readonly TestWorldFixture world;

    public CommunityGoalScriptTests()
    {
        world = new TestWorldFixture();
    }

    public void Dispose() => world.Dispose();

    [Fact]
    public void Base_Define_ReturnsParsedDefinition()
    {
        var goal = new CommunityGoal
        {
            Id = 1,
            Name = "Milestone",
            PropertiesJson = """
                {
                  "requirements": [
                    { "key": "logs", "type": "Item", "item": 120, "target": 5000 },
                    { "key": "gold", "type": "Gold", "target": 1000000 }
                  ]
                }
                """
        };

        var def = new BaseCommunityGoalScript().Define(goal, world.World);

        Assert.Equal(2, def.Requirements.Count);
        Assert.Equal("logs", def.Requirements[0].Key);
        Assert.Equal(RequirementKind.Item, def.Requirements[0].Kind);
        Assert.Equal(120, def.Requirements[0].ItemTemplateId);
        Assert.Equal(5000, def.Requirements[0].Target);
        Assert.Equal(RequirementKind.Gold, def.Requirements[1].Kind);
    }

    [Fact]
    public void GetSummaryText_TargetedGoals_IsIntegerPercentOfTargetedRequirements()
    {
        var goal = new CommunityGoal
        {
            Id = 1,
            Name = "Milestone",
            PropertiesJson = """
                {
                  "requirements": [
                    { "key": "logs", "type": "Item", "item": 120, "target": 100 },
                    { "key": "gold", "type": "Gold", "target": 200 },
                    { "key": "votes", "type": "Custom", "label": "Votes" }
                  ]
                }
                """
        };
        goal.Definition = new BaseCommunityGoalScript().Define(goal, world.World);

        var run = new CommunityGoalRun { GoalId = goal.Id, Run = 1 };
        run.Totals["logs"] = 72;
        run.Totals["gold"] = 144;
        run.Totals["votes"] = 99999;

        Assert.Equal("72%", new BaseCommunityGoalScript().GetSummaryText(goal, run, world.World));
    }

    [Fact]
    public void GetSummaryText_TargetedGoals_CapsOvershootAt100Percent()
    {
        var goal = new CommunityGoal
        {
            Id = 1,
            Name = "Milestone",
            PropertiesJson = """
                {
                  "requirements": [
                    { "key": "logs", "type": "Item", "item": 120, "target": 100 },
                    { "key": "gold", "type": "Gold", "target": 200 }
                  ]
                }
                """
        };
        goal.Definition = new BaseCommunityGoalScript().Define(goal, world.World);

        var run = new CommunityGoalRun { GoalId = goal.Id, Run = 1 };
        run.Totals["logs"] = 150;
        run.Totals["gold"] = 250;

        Assert.Equal("100%", new BaseCommunityGoalScript().GetSummaryText(goal, run, world.World));
    }

    [Fact]
    public void GetSummaryText_UntargetedGoals_IsFirstRequirementTotalWithLabel()
    {
        var goal = new CommunityGoal
        {
            Id = 1,
            Name = "Pot",
            PropertiesJson = """
                {
                  "requirements": [
                    { "key": "gold", "type": "Gold", "label": "gold" },
                    { "key": "tickets", "type": "Custom", "label": "tickets" }
                  ]
                }
                """
        };
        goal.Definition = new BaseCommunityGoalScript().Define(goal, world.World);

        var run = new CommunityGoalRun { GoalId = goal.Id, Run = 1 };
        run.Totals["gold"] = 184000;
        run.Totals["tickets"] = 3;

        Assert.Equal("184,000 gold", new BaseCommunityGoalScript().GetSummaryText(goal, run, world.World));
    }

    [Fact]
    public void CompiledScript_WithoutCommunityGoalsUsing_Compiles()
    {
        var script = world.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new() { Requirements = new[] { Requirement.Custom(""votes"", ""Votes"", 1000) } };
}
return typeof(G);
");

        var goal = new CommunityGoal { Id = 1, Name = "G", PropertiesJson = "{}" };
        var def = script.Object.Define(goal, world.World);

        var req = Assert.Single(def.Requirements);
        Assert.Equal(RequirementKind.Custom, req.Kind);
        Assert.Equal("votes", req.Key);
        Assert.Equal(1000, req.Target);
    }
}
