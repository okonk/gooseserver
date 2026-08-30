using Goose;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

public class DimensionCommandRegistrationTests
{
    private const int Offset = 100000;
    private const int StartMapId = 1;

    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(StartMapId, "Town", width: 100, height: 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(StartMapId)!, 5, 5);
        player.Properties["dimension.max"] = 6;
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3)!;
        player.Level = 50;
        player.Sent.Clear();

        return (fixture, player);
    }

    [Fact]
    public void Dimension_command_still_warps_through_the_new_registration_path()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Experience = 200_000_000_000L;

        Assert.True(fixture.RunCommand(player, "/dimension 5"));

        Assert.Equal(StartMapId + Offset * 5, player.MapID);
    }

    /// <summary>The key is "/dimension " with a trailing space, so bare input cannot match
    /// the trie - the packet is not handled at all, exactly as legacy.</summary>
    [Fact]
    public void Bare_dimension_input_is_not_handled()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        Assert.False(fixture.RunCommand(player, "/dimension"));
        Assert.Empty(player.Sent);
        Assert.Equal(StartMapId, player.MapID);
    }

    /// <summary>The in-body range check is kept: 7 parses fine, the handler refuses it with
    /// the legacy range line.</summary>
    [Fact]
    public void Out_of_range_dimension_sends_the_legacy_range_line()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        Assert.True(fixture.RunCommand(player, "/dimension 7"));

        Assert.Equal(StartMapId, player.MapID);
        Assert.Contains(player.Sent, m => m.Contains("/dimension <0-6>"));
    }

    /// <summary>Intended delta from legacy: unparseable input is rejected by the binder
    /// with the framework usage line, before the handler runs.</summary>
    [Fact]
    public void Unparseable_dimension_argument_sends_framework_usage()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        Assert.True(fixture.RunCommand(player, "/dimension abc"));

        Assert.Equal(StartMapId, player.MapID);
        Assert.Contains(player.Sent, m => m.Contains("Usage: /dimension <dim>"));
    }

    [Fact]
    public void GiveSpirit_transfers_between_two_online_players()
    {
        var (fixture, alice) = Loaded();
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(StartMapId)!, 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;
        spirit.Add(alice, 100, fixture.World);

        Assert.True(fixture.RunCommand(alice, "/givesp Bob 10"));

        Assert.Equal(90, spirit.GetBalance(alice));
        Assert.Equal(10, spirit.GetBalance(bob));
        Assert.Contains(alice.Sent, m => m.Contains("You give 10"));
        Assert.Contains(bob.Sent, m => m.Contains("gives you 10"));
    }

    [Fact]
    public void Non_command_event_registration_still_dispatches()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        fixture.World.EventHandler.RegisterEvent("GID", (p, d) => new GidStubEvent { Player = p, Data = d });

        Assert.True(fixture.RunCommand(player, "GID"));
        Assert.Contains(player.Sent, m => m.Contains("gid ran"));
    }

    /// <summary>Command keys are refused by RegisterEvent: nothing is registered, the
    /// packet is not handled by either trie, and nothing is sent.</summary>
    [Fact]
    public void Slash_key_via_RegisterEvent_is_not_dispatchable()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        fixture.World.EventHandler.RegisterEvent("/sneaky ", (p, d) => new GidStubEvent { Player = p, Data = d });

        Assert.False(fixture.RunCommand(player, "/sneaky "));
        Assert.Empty(player.Sent);
    }
}

internal sealed class GidStubEvent : Event
{
    public override void Ready(GameWorld world) => world.Send(this.Player, "gid ran");
}
