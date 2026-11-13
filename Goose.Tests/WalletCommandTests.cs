using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class WalletCommandTests
{
    private sealed class Ctx : IDisposable
    {
        public TestWorldFixture Fixture { get; }
        public TestWorldFixture.CapturingPlayer Player { get; }

        public Ctx()
        {
            this.Fixture = new TestWorldFixture();
            var map = this.Fixture.AddBaseMap(1, "m", 60, 40);
            this.Player = this.Fixture.CommandPlayerOn(map, 5, 5);
            map.AddPlayer(this.Player, this.Fixture.World);
        }

        public void Dispose() => this.Fixture.Dispose();
    }

    [Fact]
    public void Wallet_lists_gold_and_credits_with_balances()
    {
        using var ctx = new Ctx();
        ctx.Player.Gold = 1234;
        ctx.Player.Credits = 7;

        Assert.True(ctx.Fixture.RunCommand(ctx.Player, "/wallet"));

        Assert.Contains(ctx.Player.Sent, s => s.StartsWith("MKW") && s.Contains(",Wallet,"));
        var lines = ctx.Player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Contains(",Gold: 1,234|0|0|20398|820100|*", lines[0]);
        Assert.Contains(",Credits: 7|0|0|30000|5|*", lines[1]);
    }

    [Fact]
    public void Wallet_includes_registered_data_currency_with_icon()
    {
        using var ctx = new Ctx();
        var currency = new DataCurrency("arena_coins", "Arena Coins", "ac", new CurrencyIcon(5, 7), true);
        ctx.Fixture.World.CurrencyHandler.Register(currency);
        currency.Add(ctx.Player, 40, null);

        Assert.True(ctx.Fixture.RunCommand(ctx.Player, "/wallet"));

        var lines = ctx.Player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Contains(lines, s => s.Contains(",Arena Coins: 40|0|0|5|7|*"));
    }

    [Fact]
    public void Wallet_line_click_closes_without_side_effects()
    {
        using var ctx = new Ctx();
        ctx.Player.Gold = 1234;

        Assert.True(ctx.Fixture.RunCommand(ctx.Player, "/wallet"));
        var window = Assert.Single(ctx.Player.Windows);
        int sentBeforeClick = ctx.Player.Sent.Count;

        window.LineClicked(1, 0, ctx.Player, ctx.Fixture.World);

        Assert.Empty(ctx.Player.Windows);
        var afterClick = ctx.Player.Sent.Skip(sentBeforeClick).ToArray();
        Assert.Equal([P.CloseWindow(window.ID)], afterClick);
        Assert.Equal(1234, ctx.Player.Gold);
    }
}
