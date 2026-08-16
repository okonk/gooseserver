using System;
using Goose;

/// <summary>Spirit, the dimension currency. The wallet is BaseStats.SP, which already
/// persists as players.player_sp, so no schema change is needed.
///
/// MaxStats.SP is separate accounting from BaseStats.SP: MaxSP reads MaxStats
/// (Player.cs:210), and CurrentSP's setter clamps to MaxSP (Player.cs:185). So a balance
/// change moves both, and CurrentSP is topped up afterwards - regen is zeroed in
/// GooseSettings.json, so nothing else ever moves it.
///
/// Gear granting SP raises MaxStats only, so it cannot inflate the balance. It can make
/// MaxSP exceed the balance, which is cosmetic in the client's SP bar.</summary>
public class SpiritCurrency : ICurrency
{
    public string Id { get { return Dimensions.SpiritCurrencyId; } }
    public string Name { get { return "spirit"; } }
    public string ShortName { get { return "sp"; } }

    public long GetBalance(Player player) { return player.BaseStats.SP; }

    public long GetBuyPrice(ItemTemplate template, int stack) { return template.Value * stack; }

    /// <summary>Half value, and a refusal for worthless items - a dimension clone of a
    /// zero-value base item (0 x 3^dim = 0) must be refused like gold refuses it, or the
    /// vendor would take the item and pay nothing.</summary>
    public long GetSellPrice(Item item, int stack)
    {
        if (item.Value == 0) return -1;
        return stack * item.Value / 2;
    }

    public void Add(Player player, long amount, GameWorld world)
    {
        player.BaseStats.SP += amount;

        var delta = new AttributeSet();
        delta.SP = amount;
        player.AddStats(delta, world);        // raises MaxStats.SP and sends StatusInfo

        player.CurrentSP = player.MaxSP;      // setter clamps, so this must follow AddStats
        world.Send(player, P.StatusInfo(player));
    }

    public void Remove(Player player, long amount, GameWorld world)
    {
        player.BaseStats.SP -= amount;

        var delta = new AttributeSet();
        delta.SP = amount;
        player.RemoveStats(delta, world);

        player.CurrentSP = player.MaxSP;
        world.Send(player, P.StatusInfo(player));
    }
}
