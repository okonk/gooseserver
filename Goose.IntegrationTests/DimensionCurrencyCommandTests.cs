using System.Linq;
using Goose;
using Goose.IntegrationTests.Collections;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionCurrencyCommandTests
{
    private const long GoldPerSpirit = 1_000_000;
    private const long ExpPerSpiritPurchase = 25_000_000;

    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded(
        long spiritBalance = 100)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 5, 5, "Alice");
        // Not a commoner: /buyexperience refuses class 1.
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        fixture.RegisterOnlinePlayer(player);
        fixture.World.CurrencyHandler.Get("spirit").Add(player, spiritBalance, fixture.World);
        player.Sent.Clear();

        return (fixture, player);
    }

    private static long Spirit(GlobalScriptFixture fixture, Player player)
        => fixture.World.CurrencyHandler.Get("spirit").GetBalance(player);

    // ---- /buygold -------------------------------------------------------

    [Fact]
    public void BuyGold_trades_spirit_for_gold_at_the_configured_rate()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        player.Gold = 0;

        Assert.True(fixture.RunCommand(player, "/buygold 3"));

        Assert.Equal(7, Spirit(fixture, player));
        Assert.Equal(3 * GoldPerSpirit, player.Gold);
        Assert.Single(fixture.World.LogHandler.Pending, l => l.Type == Log.Types.BuyGold);
    }

    [Theory]
    [InlineData("/buygold ")]
    [InlineData("/buygold abc")]
    [InlineData("/buygold 0")]
    [InlineData("/buygold -5")]
    public void BuyGold_refuses_bad_amounts_and_charges_nothing(string command)
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        player.Gold = 0;

        fixture.RunCommand(player, command);

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(0, player.Gold);
        Assert.Empty(fixture.World.LogHandler.Pending.Where(l => l.Type == Log.Types.BuyGold));
    }

    /// <summary>One past the guard threshold (long.MaxValue / GoldPerSpirit = 9_223_372_036):
    /// without the guard, granted = amount * GoldPerSpirit wraps negative and
    /// GoldCurrency.Add does Gold += amount unguarded (Player.cs:1482), corrupting the
    /// gold balance. The balance must be at least the amount, or the balance check refuses
    /// first and the guard is never reached - long.MaxValue has that property for every
    /// balance, which is why this needs its own fact rather than a theory row.</summary>
    [Fact]
    public void BuyGold_refuses_an_amount_that_would_overflow_the_multiply()
    {
        var (fixture, player) = Loaded(spiritBalance: 9_223_372_036_855L);
        using var _ = fixture;
        player.Gold = 0;

        fixture.RunCommand(player, "/buygold 9223372036855");

        Assert.Equal(9_223_372_036_855L, Spirit(fixture, player));
        Assert.Equal(0, player.Gold);
        Assert.Contains(player.Sent, m => m.Contains("That is more gold than exists"));
    }

    [Fact]
    public void BuyGold_refuses_an_insufficient_balance_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 2);
        using var _ = fixture;
        player.Gold = 0;

        fixture.RunCommand(player, "/buygold 3");

        Assert.Equal(2, Spirit(fixture, player));
        Assert.Equal(0, player.Gold);
        Assert.Contains(player.Sent, m => m.Contains("Not enough spirit"));
    }

    // ---- /buyexperience -------------------------------------------------

    [Fact]
    public void BuyExperience_grants_exactly_the_unmodified_amount()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 0;
        player.Experience = 0;

        Assert.True(fixture.RunCommand(player, "/buyexperience 2"));

        Assert.Equal(8, Spirit(fixture, player));
        Assert.Equal(2 * ExpPerSpiritPurchase, player.Experience);
    }

    /// <summary>The modifier must not touch purchased experience — that is the entire
    /// reason for Task 1's applyModifiers overload. Both branches of the two-branch scaling
    /// (Player.cs:1662-1671) are covered: limit 0 takes the full-modifier branch, a limit
    /// the player is already past takes the reduced one.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1_000)]
    public void BuyExperience_is_unaffected_by_the_world_experience_modifier(int modifierLimit)
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 0;
        GameWorld.Settings.ExperienceModifier = 2;
        GameWorld.Settings.ExperienceModifierLimit = modifierLimit;
        fixture.World.ExperienceModifier = 2;
        player.Experience = 50_000;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(50_000 + ExpPerSpiritPurchase, player.Experience);
    }

    [Fact]
    public void BuyExperience_refuses_commoners_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        player.ClassID = 1;
        player.Class = fixture.World.ClassHandler.GetClass(1);
        player.Experience = 0;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(0, player.Experience);
        Assert.Contains(player.Sent, m => m.Contains("Choose a class"));
    }

    /// <summary>AddExperience early-returns over the cap (Player.cs:1653), which would take
    /// the spirit and grant nothing.</summary>
    [Fact]
    public void BuyExperience_refuses_when_already_over_the_cap_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 1_000_000;
        player.Experience = 2_000_000;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(2_000_000, player.Experience);
    }

    /// <summary>The prospective check, and the reason the current-total check alone is not
    /// enough. A player one experience under the cap passes "am I over the cap?" and then
    /// buys 25,000,000 — landing 24,999,999 above a ceiling the server is supposed to
    /// enforce. The cap has to be tested against the total the purchase would produce, not
    /// the total the player has now.</summary>
    [Fact]
    public void BuyExperience_refuses_a_purchase_that_would_cross_the_cap_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 100_000_000;
        player.Experience = 99_999_999;
        player.ExperienceSold = 0;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(99_999_999, player.Experience);
        Assert.Contains(player.Sent, m => m.Contains("experience cap"));
    }

    /// <summary>The largest purchase that still lands on or under the cap must go through —
    /// a prospective check that also refuses the legitimate last purchase is a regression,
    /// not a fix.</summary>
    [Fact]
    public void BuyExperience_allows_a_purchase_that_lands_exactly_on_the_cap()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 100_000_000;
        player.Experience = 100_000_000 - ExpPerSpiritPurchase;
        player.ExperienceSold = 0;

        Assert.True(fixture.RunCommand(player, "/buyexperience 1"));

        Assert.Equal(9, Spirit(fixture, player));
        Assert.Equal(100_000_000, player.Experience);
    }

    /// <summary>amount * ExpPerSpiritPurchase is a long multiply on an amount the player
    /// controls. Without a guard it wraps negative and AddExperience subtracts.</summary>
    [Fact]
    public void BuyExperience_refuses_an_amount_that_would_overflow_the_multiply()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 0;
        player.Experience = 0;

        fixture.RunCommand(player, "/buyexperience 9223372036854775807");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(0, player.Experience);
    }

    // ---- /givesp --------------------------------------------------------

    [Fact]
    public void GiveSp_moves_the_balance_between_two_players()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        Assert.True(fixture.RunCommand(alice, "/givesp Bob 40"));

        Assert.Equal(60, Spirit(fixture, alice));
        Assert.Equal(40, Spirit(fixture, bob));
        Assert.Contains(bob.Sent, m => m.Contains("Alice"));
    }

    /// <summary>Both sides of a transfer, joinable. otherid carries the counterparty's
    /// PlayerID and the text carries before/after for each wallet, so an audit can prove a
    /// transfer conserved spirit without replaying every log in between.</summary>
    [Fact]
    public void GiveSp_logs_both_sides_with_the_counterparty_and_balances()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        bob.PlayerID = 77;
        fixture.RegisterOnlinePlayer(bob);

        fixture.RunCommand(alice, "/givesp Bob 40");

        var entries = fixture.World.LogHandler.Pending
            .Where(l => l.Type == Log.Types.GiveSpirit).ToList();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, l => l.PlayerID == alice.PlayerID && l.OtherID == 77
                                      && l.Text.Contains("100 -> 60"));
        Assert.Contains(entries, l => l.PlayerID == 77 && l.OtherID == alice.PlayerID
                                      && l.Text.Contains("0 -> 40"));
    }

    [Theory]
    [InlineData("/givesp ")]
    [InlineData("/givesp Bob")]
    [InlineData("/givesp Bob abc")]
    [InlineData("/givesp Bob 0")]
    [InlineData("/givesp Bob -5")]
    public void GiveSp_refuses_bad_arguments_and_moves_nothing(string command)
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        fixture.RunCommand(alice, command);

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Equal(0, Spirit(fixture, bob));
    }

    [Fact]
    public void GiveSp_refuses_an_offline_target_and_moves_nothing()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;

        fixture.RunCommand(alice, "/givesp Nobody 40");

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Contains(alice.Sent, m => m.Contains("not online"));
    }

    /// <summary>Self-transfer is not a no-op if it is allowed: Remove then Add both run
    /// AddStats/RemoveStats against the same wallet, and any asymmetry between them mints
    /// or burns spirit.</summary>
    [Fact]
    public void GiveSp_refuses_a_self_transfer()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;

        fixture.RunCommand(alice, "/givesp Alice 40");

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Contains(alice.Sent, m => m.Contains("yourself"));
    }

    [Fact]
    public void GiveSp_refuses_an_insufficient_balance_and_moves_nothing()
    {
        var (fixture, alice) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        fixture.RunCommand(alice, "/givesp Bob 40");

        Assert.Equal(10, Spirit(fixture, alice));
        Assert.Equal(0, Spirit(fixture, bob));
        Assert.Contains(alice.Sent, m => m.Contains("Not enough spirit"));
    }

    /// <summary>The recipient side. BaseStats.SP is a long (AttributeSet.cs:16), so a
    /// transfer into an already-huge wallet wraps negative and destroys the balance — and a
    /// wallet past MaxSpiritBalance is past what the rest of the economy was sized for.
    /// The check is on the recipient, before either side moves.</summary>
    [Fact]
    public void GiveSp_refuses_when_the_recipient_would_exceed_the_cap_and_moves_nothing()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);
        // One under the cap, so any positive transfer crosses it.
        fixture.World.CurrencyHandler.Get("spirit").Add(bob, 1_000_000_000_000L - 1, fixture.World);

        fixture.RunCommand(alice, "/givesp Bob 40");

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Equal(1_000_000_000_000L - 1, Spirit(fixture, bob));
        Assert.Contains(alice.Sent, m => m.Contains("cannot hold"));
    }

    /// <summary>The underflow corner of the recipient check: with amount above
    /// MaxSpiritBalance, MaxSpiritBalance - amount goes negative, so the comparison only
    /// refuses because a non-negative balance is always greater than a negative bound.
    /// That is the direction the guard leans on, so it needs its own pin.</summary>
    [Fact]
    public void GiveSp_refuses_a_transfer_larger_than_the_wallet_cap_and_moves_nothing()
    {
        var (fixture, alice) = Loaded(spiritBalance: 2_000_000_000_000L);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        fixture.RunCommand(alice, "/givesp Bob 2000000000000");

        Assert.Equal(2_000_000_000_000L, Spirit(fixture, alice));
        Assert.Equal(0, Spirit(fixture, bob));
        Assert.Contains(alice.Sent, m => m.Contains("cannot hold"));
    }
}
