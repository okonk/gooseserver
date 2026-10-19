namespace Goose.Tests;

public class PlayerScriptStateTests
{
    [Fact]
    public void ScriptState_starts_empty_and_holds_values()
    {
        var player = new Player(0);

        Assert.Empty(player.ScriptState);

        player.ScriptState["ambush_cooldown_until"] = 1_234_567;
        player.ScriptState["stalker_grace"] = 42;

        Assert.Equal(1_234_567, player.ScriptState["ambush_cooldown_until"]);
        Assert.Equal(42, player.ScriptState["stalker_grace"]);
    }

    [Fact]
    public void ScriptState_does_not_leak_into_properties()
    {
        var player = new Player(0);

        player.ScriptState["ambush_cooldown_until"] = 999;

        string saved = JsonHelper.Serialize(player.Properties.Clone());

        Assert.DoesNotContain("ambush_cooldown_until", saved);
        Assert.DoesNotContain("999", saved);
    }
}
