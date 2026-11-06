using Goose;
using Goose.Events;
using Goose.Quests;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class NpcClickHookTests
{
    private static (TestWorldFixture World, TestWorldFixture.CapturingPlayer Player, NPC Npc) Setup(
        TestWorldFixture world, Script<INPCScript> script)
    {
        var map = world.AddBaseMap(1, "Test", 10, 10);
        var player = world.CommandPlayerOn(map, 5, 5, "Tester");
        player.Windows = new List<Window>();
        world.AddOnlinePlayer(player);

        var template = world.AddNpcTemplate(100, "Click NPC", t =>
        {
            t.Quests = [MakeQuest()];
            t.Script = script;
        });
        var npc = world.SpawnNpc(map, 5, 5, template);

        return (world, player, npc);
    }

    private static Script<INPCScript> CompileNpcScript(TestWorldFixture world, string body, string fileName)
    {
        Directory.CreateDirectory(Path.Combine(world.DataDirectory, "Scripts", "NPC"));
        var relativePath = "Scripts/NPC/" + fileName;
        File.WriteAllText(Path.Combine(world.DataDirectory, relativePath), body);
        return world.World.ScriptHandler.GetScript<INPCScript>(relativePath);
    }

    private static Quest MakeQuest() => new()
    {
        Id = 77,
        Name = "Test Quest",
        Description = "d",
        FailText = "f",
        PassText = "p",
        MinLevel = 0,
        MinExperience = 0,
        Repeatable = false,
        ShowProgress = true,
    };

    [Fact]
    public void Script_handles_click_and_suppresses_quest_window()
    {
        var world = new TestWorldFixture();
        var script = CompileNpcScript(world, @"
using Goose;
using Goose.Scripting;
public class ClickScript : BaseNPCScript
{
    public override bool OnPlayerClicked(NPC npc, Player player, GameWorld world)
    {
        npc.ScriptStore = ""clicked"";
        return true;
    }
}
return typeof(ClickScript);", "Click.csx");
        var (_, player, npc) = Setup(world, script);

        new PlayerLeftClickEvent { Player = player, Data = "LC5,5" }.Ready(world.World);

        Assert.Equal("clicked", npc.ScriptStore);
        Assert.DoesNotContain(player.Windows, w => w.Type == Window.WindowTypes.Quest);
    }

    [Fact]
    public void Script_returning_false_falls_through_to_quests()
    {
        var world = new TestWorldFixture();
        var script = CompileNpcScript(world, @"
using Goose;
using Goose.Scripting;
public class FalseScript : BaseNPCScript
{
    public override bool OnPlayerClicked(NPC npc, Player player, GameWorld world) => false;
}
return typeof(FalseScript);", "False.csx");
        var (_, player, npc) = Setup(world, script);

        new PlayerLeftClickEvent { Player = player, Data = "LC5,5" }.Ready(world.World);

        Assert.Contains(player.Windows, w => w.Type == Window.WindowTypes.Quest);
    }

    [Fact]
    public void Throwing_script_falls_through()
    {
        var world = new TestWorldFixture();
        var script = CompileNpcScript(world, @"
using Goose;
using Goose.Scripting;
public class ThrowScript : BaseNPCScript
{
    public override bool OnPlayerClicked(NPC npc, Player player, GameWorld world)
    {
        throw new Exception(""boom"");
    }
}
return typeof(ThrowScript);", "Throw.csx");
        var (_, player, npc) = Setup(world, script);

        new PlayerLeftClickEvent { Player = player, Data = "LC5,5" }.Ready(world.World);

        Assert.Contains(player.Windows, w => w.Type == Window.WindowTypes.Quest);
    }

    [Fact]
    public void Click_on_another_tile_does_not_invoke_script()
    {
        var world = new TestWorldFixture();
        var script = CompileNpcScript(world, @"
using Goose;
using Goose.Scripting;
public class ClickScript2 : BaseNPCScript
{
    public override bool OnPlayerClicked(NPC npc, Player player, GameWorld world)
    {
        npc.ScriptStore = ""clicked"";
        return true;
    }
}
return typeof(ClickScript2);", "Click2.csx");
        var (_, player, npc) = Setup(world, script);

        new PlayerLeftClickEvent { Player = player, Data = "LC6,6" }.Ready(world.World);

        Assert.Null(npc.ScriptStore);
        Assert.DoesNotContain(player.Windows, w => w.Type == Window.WindowTypes.Quest);
    }
}
