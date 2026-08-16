using System;
using Goose;

/// <summary>Handles "/dimension <n>". Registered with a trailing space so the command
/// trie matches it as a longest-prefix, exactly like "/tell " and "/warp "
/// (EventHandler.cs:123).</summary>
public class DimensionCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new DimensionCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        int dim;
        if (tokens.Length < 2 || !int.TryParse(tokens[1], out dim) || dim < 0 || dim > Dimensions.DimensionCount)
        {
            world.Send(this.Player, P.ServerMessage("/dimension <0-" + Dimensions.DimensionCount + ">"));
            return;
        }

        int max = DimensionHelpers.MaxDimensionOf(this.Player);
        if (dim > max)
        {
            world.Send(this.Player, P.ServerMessage(DimensionHelpers.MaxDimensionRefusal(max)));
            return;
        }

        var target = world.MapHandler.GetMap(Dimensions.StartMapId + Dimensions.Offset * dim);
        if (target == null)
        {
            world.Send(this.Player, P.ServerMessage("That dimension does not exist."));
            return;
        }

        // PlayerCanJoin, then WarpTo. Player.WarpTo (Player.cs:1234) does no gating of its
        // own - MoveEvent (:123), SpellEffect (:831) and DimensionTeleport.csx (:61) each
        // call PlayerCanJoin first, and this command has to as well or every map-level
        // gate in this feature (MinLevel, Min/MaxExperience, required items, and
        // DimensionMap.csx's own hook) is bypassed by the one route players actually use.
        //
        // PlayerCanJoin sends its own refusal, so there is nothing to say here.
        if (!target.PlayerCanJoin(this.Player, world)) return;

        this.Player.WarpTo(world, target, Dimensions.WardenX, Dimensions.WardenY);
    }
}

/// <summary>Handles "/resetitem &lt;n&gt;": rerolls one dimension equipment item's suffix
/// and rarity for ResetItemCostBase^dim spirit. Registered with a trailing space so the
/// command trie matches it as a longest-prefix, exactly like "/dimension ".</summary>
public class ResetItemCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new ResetItemCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        int slotId;
        if (tokens.Length < 2 || !int.TryParse(tokens[1], out slotId) ||
            slotId < 1 || slotId > GameWorld.Settings.InventorySize)
        {
            world.Send(this.Player, P.ServerMessage(
                "/resetitem <1-" + GameWorld.Settings.InventorySize + "> - rerolls a dimension item's suffix."));
            return;
        }

        var slot = this.Player.Inventory.GetSlot(slotId);
        if (slot == null || slot.Item == null)
        {
            world.Send(this.Player, P.ServerMessage("No item exists at that inventory slot."));
            return;
        }

        var item = slot.Item;

        // One Item object backs the whole stack (ItemSlot.cs:17-19), so rerolling a stack
        // of two would rewrite both for one charge. Refuse rather than split.
        if (slot.Stack != 1)
        {
            world.Send(this.Player, P.ServerMessage("Only a single item can be reset, not a stack."));
            return;
        }

        // Three separate questions, and all three have to be asked. The division alone
        // says nothing: a sheet-authored template with an id above Offset would divide to a
        // plausible-looking dimension, be priced with Math.Pow against a dimension that may
        // not exist, and be charged for a reroll on an item the dimension scripts never made.
        int dim = DimensionHelpers.DimensionOf(item.TemplateID);
        if (dim < 1 || dim > Dimensions.DimensionCount)
        {
            world.Send(this.Player, P.ServerMessage("Only items from a higher plane can be reset."));
            return;
        }

        // CloneItemTemplates registers each clone at baseId + Offset*dim over a base that
        // exists, and stamps the dimension script onto it. All three must hold, or this is
        // not a generated clone and does not belong here.
        var registered = world.ItemHandler.GetTemplate(item.TemplateID);
        if (registered == null || registered != item.Template ||
            world.ItemHandler.GetTemplate(DimensionHelpers.BaseId(item.TemplateID)) == null ||
            registered.Script == null)
        {
            world.Send(this.Player, P.ServerMessage("Only items from a higher plane can be reset."));
            return;
        }

        // Dimension tomes are Scroll consumables; nothing but gear carries modifiers.
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
        {
            world.Send(this.Player, P.ServerMessage("Only weapons and armor can be reset."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long cost = (long)Math.Pow(Dimensions.ResetItemCostBase, dim);

        // The balance check is the guard, not a nicety: Part 5 established that Remove
        // does not itself refuse an overdraft.
        long before = spirit.GetBalance(this.Player);
        if (before < cost)
        {
            world.Send(this.Player, P.ServerMessage(
                "Not enough " + spirit.Name + " to reset this item. (" + cost + ")"));
            return;
        }

        world.ItemHandler.ResetModifiers(item);
        try
        {
            DimensionRolls.Reroll(item, world);
        }
        catch
        {
            // A roll that throws mid-apply has left the item off template state. The
            // charge below never runs, so the player's cost is the modifiers the reset
            // stripped - say so, then rethrow so the event loop's catch
            // (EventHandler.cs:373) still logs it rather than this block swallowing it.
            world.Send(this.Player, P.ServerMessage("The void refused the remaking."));
            throw;
        }
        spirit.Remove(this.Player, cost, world);

        this.Player.Inventory.SendSlot(slotId, world);
        world.Send(this.Player, P.ServerMessage(
            "You spend " + cost + " " + spirit.Name + " to remake " + item.Name + "."));

        // Its own log type, not CreatedCustom: that is the GM item-creation log, and
        // folding a paid player reroll into it makes both unqueryable. otherid carries the
        // item's id so a reroll can be joined to the item it rewrote.
        world.LogHandler.Log(Log.Types.ResetItem, this.Player,
            "ResetItem: template " + item.TemplateID + " dim " + dim
            + " cost " + cost + " " + spirit.ShortName
            + " balance " + before + " -> " + (before - cost),
            item.ItemID);
    }
}

/// <summary>Handles "/buygold &lt;amount&gt;": trades spirit for gold at GoldPerSpirit
/// each. Registered with a trailing space so the command trie matches it as a
/// longest-prefix, exactly like "/dimension ".</summary>
public class BuyGoldCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new BuyGoldCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (!Dimensions.TryParseAmount(tokens, 1, out amount))
        {
            world.Send(this.Player, P.ServerMessage(
                "/buygold <amount> - trades spirit for gold at "
                + Dimensions.GoldPerSpirit.ToString("N0") + " each."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        var gold = world.CurrencyHandler.Get(Currency.Gold);   // no CurrencyHandler.Gold property
        if (spirit == null || gold == null) return;

        // Before the balance check: a wrapped product would pass any check made after it.
        if (amount > long.MaxValue / Dimensions.GoldPerSpirit)
        {
            world.Send(this.Player, P.ServerMessage("That is more gold than exists."));
            return;
        }

        long before = spirit.GetBalance(this.Player);
        if (before < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        long granted = amount * Dimensions.GoldPerSpirit;

        spirit.Remove(this.Player, amount, world);
        gold.Add(this.Player, granted, world);

        world.Send(this.Player, P.ServerMessage(
            "You trade " + amount + " " + spirit.Name + " for " + granted.ToString("N0") + " gold."));
        world.LogHandler.Log(Log.Types.BuyGold, this.Player,
            "BuyGold: " + amount + " " + spirit.ShortName + " -> " + granted + " gold"
            + ", spirit " + before + " -> " + (before - amount));
    }
}

/// <summary>Handles "/buyexperience &lt;amount&gt;": buys experience at
/// ExpPerSpiritPurchase each, unmodified by the world's experience modifier. Registered
/// with a trailing space so the command trie matches it as a longest-prefix, exactly like
/// "/dimension ".</summary>
public class BuyExperienceCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new BuyExperienceCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (!Dimensions.TryParseAmount(tokens, 1, out amount))
        {
            world.Send(this.Player, P.ServerMessage(
                "/buyexperience <amount> - buys experience at "
                + Dimensions.ExpPerSpiritPurchase.ToString("N0") + " each."));
            return;
        }

        if (this.Player.ClassID == 1)
        {
            world.Send(this.Player, P.ServerMessage("Choose a class before you buy experience."));
            return;
        }

        if (amount > long.MaxValue / Dimensions.ExpPerSpiritPurchase)
        {
            world.Send(this.Player, P.ServerMessage("That is more experience than exists."));
            return;
        }

        long granted = amount * Dimensions.ExpPerSpiritPurchase;
        long total = this.Player.Experience + this.Player.ExperienceSold;

        // Prospective, not current. AddExperience early-returns when the CURRENT total is
        // over the cap (Player.cs:1653-1660), so checking the same condition here only
        // catches players who are already past it - a player one experience under the cap
        // passes, buys, and lands 24,999,999 above a ceiling the server is meant to
        // enforce. Test what the purchase would produce.
        if (GameWorld.Settings.ExperienceCap > 0 && total + granted > GameWorld.Settings.ExperienceCap)
        {
            long affordable = (GameWorld.Settings.ExperienceCap - total) / Dimensions.ExpPerSpiritPurchase;
            world.Send(this.Player, P.ServerMessage(affordable > 0
                ? "That would carry you past the experience cap. You can buy at most " + affordable + "."
                : "You have reached the experience cap."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long before = spirit.GetBalance(this.Player);
        if (before < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        spirit.Remove(this.Player, amount, world);
        this.Player.AddExperience(granted, world, Player.ExperienceMessage.Normal, applyModifiers: false);

        world.Send(this.Player, P.ServerMessage(
            "You spend " + amount + " " + spirit.Name + " to gain " + granted.ToString("N0") + " experience."));
        world.LogHandler.Log(Log.Types.BuyExperience, this.Player,
            "BuyExperience: " + amount + " " + spirit.ShortName + " -> " + granted + " exp"
            + ", spirit " + before + " -> " + (before - amount));
    }
}

/// <summary>Handles "/givesp &lt;player&gt; &lt;amount&gt;": transfers spirit between two
/// online players. Registered with a trailing space so the command trie matches it as a
/// longest-prefix, exactly like "/dimension ".</summary>
public class GiveSpiritCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new GiveSpiritCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (tokens.Length < 3 || !Dimensions.TryParseAmount(tokens, 2, out amount))
        {
            world.Send(this.Player, P.ServerMessage("/givesp <player> <amount>"));
            return;
        }

        var target = world.PlayerHandler.GetPlayer(tokens[1]);
        if (target == null || target.State != Player.States.Ready)
        {
            world.Send(this.Player, P.ServerMessage(tokens[1] + " is not online."));
            return;
        }

        if (target == this.Player)
        {
            world.Send(this.Player, P.ServerMessage("You cannot give spirit to yourself."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long senderBefore = spirit.GetBalance(this.Player);
        if (senderBefore < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        // The recipient side, checked before either wallet moves. BaseStats.SP is a long,
        // so a transfer into a large enough wallet wraps negative; MaxSpiritBalance keeps
        // the refusal well short of that and makes a faucet bug visible as a refusal
        // rather than as a corrupted balance.
        long targetBefore = spirit.GetBalance(target);
        if (targetBefore > Dimensions.MaxSpiritBalance - amount)
        {
            world.Send(this.Player, P.ServerMessage(target.Name + " cannot hold that much " + spirit.Name + "."));
            return;
        }

        spirit.Remove(this.Player, amount, world);
        spirit.Add(target, amount, world);

        world.Send(this.Player, P.ServerMessage(
            "You give " + amount + " " + spirit.Name + " to " + target.Name + "."));
        world.Send(target, P.ServerMessage(
            this.Player.Name + " gives you " + amount + " " + spirit.Name + "."));

        // One entry per side, each naming the counterparty in otherid and carrying its own
        // before/after. Two rows rather than one because logs are queried per player.
        world.LogHandler.Log(Log.Types.GiveSpirit, this.Player,
            "GiveSpirit: sent " + amount + " " + spirit.ShortName + " to " + target.Name
            + ", balance " + senderBefore + " -> " + (senderBefore - amount),
            target.PlayerID);
        world.LogHandler.Log(Log.Types.GiveSpirit, target,
            "GiveSpirit: received " + amount + " " + spirit.ShortName + " from " + this.Player.Name
            + ", balance " + targetBefore + " -> " + (targetBefore + amount),
            this.Player.PlayerID);
    }
}
