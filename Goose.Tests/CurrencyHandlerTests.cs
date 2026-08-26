using Goose;
using Xunit;

namespace Goose.Tests;

public class CurrencyHandlerTests
{
    /// <summary>A minimal ICurrency for registry tests. Real implementations arrive in Task 4.</summary>
    private sealed class StubCurrency : ICurrency
    {
        public StubCurrency(string id) { Id = id; }
        public string Id { get; }
        public string Name => Id;
        public string ShortName => Id;
        public long GetBalance(Player player) => 0;
        public long GetBuyPrice(ItemTemplate template, int stack) => 0;
        public long GetSellPrice(Item item, int stack) => 0;
        public void Add(Player player, long amount, GameWorld world) { }
        public void Remove(Player player, long amount, GameWorld world) { }
    }

    [Fact]
    public void Get_ReturnsARegisteredCurrency()
    {
        var handler = new CurrencyHandler();
        var spirit = new StubCurrency("spirit");

        handler.Register(spirit);

        Assert.Same(spirit, handler.Get("spirit"));
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency("spirit"));

        Assert.NotNull(handler.Get("SPIRIT"));
    }

    [Fact]
    public void Get_ReturnsNullForUnknownCurrency()
    {
        Assert.Null(new CurrencyHandler().Get("doubloons"));
    }

    /// <summary>Silently overwriting a currency would repoint every item priced in it at a
    /// different wallet. That must be loud.</summary>
    [Fact]
    public void Register_RejectsADuplicateId()
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency("spirit"));

        var ex = Assert.Throws<InvalidOperationException>(() => handler.Register(new StubCurrency("spirit")));
        Assert.Contains("spirit", ex.Message);
    }

    [Fact]
    public void Register_RejectsADuplicateIdDifferingOnlyByCase()
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency("spirit"));

        Assert.Throws<InvalidOperationException>(() => handler.Register(new StubCurrency("Spirit")));
    }

    [Fact]
    public void Register_RejectsAnEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new CurrencyHandler().Register(new StubCurrency("")));
    }

    private static CurrencyHandler HandlerWith(params string[] ids)
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency(Currency.Gold));
        foreach (var id in ids) handler.Register(new StubCurrency(id));
        return handler;
    }

    private static NPC VendorWith(string? currencyId) =>
        new NPC { NPCTemplate = new NPCTemplate { CurrencyId = currencyId } };

    [Fact]
    public void Resolve_FallsBackToGoldWhenNeitherSetsACurrency()
    {
        var handler = HandlerWith();

        var resolved = handler.Resolve(new ItemTemplate(), VendorWith(null));

        Assert.Equal(Currency.Gold, resolved.Id);
    }

    [Fact]
    public void Resolve_UsesTheVendorCurrencyWhenTheItemHasNoOverride()
    {
        var handler = HandlerWith(Currency.Credits);

        var resolved = handler.Resolve(new ItemTemplate(), VendorWith(Currency.Credits));

        Assert.Equal(Currency.Credits, resolved.Id);
    }

    /// <summary>The decision from the design: a dimension item is worth spirit wherever it
    /// is traded, including at a credit dealer.</summary>
    [Fact]
    public void Resolve_ItemOverrideBeatsTheVendorCurrency()
    {
        var handler = HandlerWith(Currency.Credits, "spirit");

        var resolved = handler.Resolve(new ItemTemplate { CurrencyId = "spirit" }, VendorWith(Currency.Credits));

        Assert.Equal("spirit", resolved.Id);
    }

    [Fact]
    public void Resolve_HandlesANullVendor()
    {
        var handler = HandlerWith("spirit");

        Assert.Equal("spirit", handler.Resolve(new ItemTemplate { CurrencyId = "spirit" }, null).Id);
    }

    /// <summary>Falling back to gold here would sell a spirit item for gold at the till.
    /// Fail loud - the event loop contains the throw (EventHandler.cs:369).</summary>
    [Fact]
    public void Resolve_ThrowsWhenTheNamedCurrencyIsNotRegistered()
    {
        var handler = HandlerWith();

        var ex = Assert.Throws<InvalidOperationException>(
            () => handler.Resolve(new ItemTemplate { CurrencyId = "doubloons" }, null));

        Assert.Contains("doubloons", ex.Message);
    }
}
