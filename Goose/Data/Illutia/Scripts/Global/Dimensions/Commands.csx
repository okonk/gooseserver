using System;
using Goose;
using Goose.Commands;

public static class DimensionCommands
{
    public static void Dimension(CommandContext ctx, int dim)
    {
        if (dim < 0 || dim > Dimensions.DimensionCount)
        {
            ctx.Send("/dimension <0-" + Dimensions.DimensionCount + ">");
            return;
        }

        int max = DimensionHelpers.MaxDimensionOf(ctx.Player);
        if (dim > max)
        {
            ctx.Send(DimensionHelpers.MaxDimensionRefusal(max));
            return;
        }

        var target = ctx.World.MapHandler.GetMap(Dimensions.StartMapId + Dimensions.Offset * dim);
        if (target == null)
        {
            ctx.Send("That dimension does not exist.");
            return;
        }

        // PlayerCanJoin, then WarpTo. Player.WarpTo (Player.cs:1234) does no gating of its
        // own - MoveEvent (:123), SpellEffect (:831) and DimensionTeleport.csx (:61) each
        // call PlayerCanJoin first, and this command has to as well or every map-level
        // gate in this feature (MinLevel, Min/MaxExperience, required items, and
        // DimensionMap.csx's own hook) is bypassed by the one route players actually use.
        //
        // PlayerCanJoin sends its own refusal, so there is nothing to say here.
        if (!target.PlayerCanJoin(ctx.Player, ctx.World)) return;

        ctx.Player.WarpTo(ctx.World, target, Dimensions.WardenX, Dimensions.WardenY);
    }

    public static void ResetItem(CommandContext ctx, int slotId)
    {
        var world = ctx.World;

        if (slotId < 1 || slotId > world.Settings.InventorySize)
        {
            ctx.Send(
                "/resetitem <1-" + world.Settings.InventorySize + "> - rerolls a dimension item's suffix.");
            return;
        }

        var slot = ctx.Player.Inventory.GetSlot(slotId);
        if (slot == null || slot.Item == null)
        {
            ctx.Send("No item exists at that inventory slot.");
            return;
        }

        var item = slot.Item;

        // One Item object backs the whole stack (ItemSlot.cs:17-19), so rerolling a stack
        // of two would rewrite both for one charge. Refuse rather than split.
        if (slot.Stack != 1)
        {
            ctx.Send("Only a single item can be reset, not a stack.");
            return;
        }

        // Three separate questions, and all three have to be asked. The division alone
        // says nothing: a sheet-authored template with an id above Offset would divide to a
        // plausible-looking dimension, be priced with Math.Pow against a dimension that may
        // not exist, and be charged for a reroll on an item the dimension scripts never made.
        int dim = DimensionHelpers.DimensionOf(item.TemplateID);
        if (dim < 1 || dim > Dimensions.DimensionCount)
        {
            ctx.Send("Only items from a higher plane can be reset.");
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
            ctx.Send("Only items from a higher plane can be reset.");
            return;
        }

        // Dimension tomes are Scroll consumables; nothing but gear carries modifiers.
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
        {
            ctx.Send("Only weapons and armor can be reset.");
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long cost = (long)Math.Pow(Dimensions.ResetItemCostBase, dim);

        // The balance check is the guard, not a nicety: Part 5 established that Remove
        // does not itself refuse an overdraft.
        long before = spirit.GetBalance(ctx.Player);
        if (before < cost)
        {
            ctx.Send("Not enough " + spirit.Name + " to reset this item. (" + cost + ")");
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
            ctx.Send("The void refused the remaking.");
            throw;
        }
        spirit.Remove(ctx.Player, cost, world);

        ctx.Player.Inventory.SendSlot(slotId, world);
        ctx.Send("You spend " + cost + " " + spirit.Name + " to remake " + item.Name + ".");

        // Its own log type, not CreatedCustom: that is the GM item-creation log, and
        // folding a paid player reroll into it makes both unqueryable. otherid carries the
        // item's id so a reroll can be joined to the item it rewrote.
        world.LogHandler.Log(Log.Types.ResetItem, ctx.Player,
            "ResetItem: template " + item.TemplateID + " dim " + dim
            + " cost " + cost + " " + spirit.ShortName
            + " balance " + before + " -> " + (before - cost),
            item.ItemID);
    }

    public static void BuyGold(CommandContext ctx, long amount)
    {
        if (amount <= 0)
        {
            ctx.Send("/buygold <amount> - trades spirit for gold at "
                + Dimensions.GoldPerSpirit.ToString("N0") + " each.");
            return;
        }

        var spirit = ctx.World.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        var gold = ctx.World.CurrencyHandler.Get(Currency.Gold);   // no CurrencyHandler.Gold property
        if (spirit == null || gold == null) return;

        // Before the balance check: a wrapped product would pass any check made after it.
        if (amount > long.MaxValue / Dimensions.GoldPerSpirit)
        {
            ctx.Send("That is more gold than exists.");
            return;
        }

        long before = spirit.GetBalance(ctx.Player);
        if (before < amount)
        {
            ctx.Send("Not enough " + spirit.Name + ".");
            return;
        }

        long granted = amount * Dimensions.GoldPerSpirit;

        spirit.Remove(ctx.Player, amount, ctx.World);
        gold.Add(ctx.Player, granted, ctx.World);

        ctx.Send("You trade " + amount + " " + spirit.Name + " for " + granted.ToString("N0") + " gold.");
        ctx.World.LogHandler.Log(Log.Types.BuyGold, ctx.Player,
            "BuyGold: " + amount + " " + spirit.ShortName + " -> " + granted + " gold"
            + ", spirit " + before + " -> " + (before - amount));
    }

    public static void BuyExperience(CommandContext ctx, long amount)
    {
        if (amount <= 0)
        {
            ctx.Send("/buyexperience <amount> - buys experience at "
                + Dimensions.ExpPerSpiritPurchase.ToString("N0") + " each.");
            return;
        }

        if (ctx.Player.ClassID == 1)
        {
            ctx.Send("Choose a class before you buy experience.");
            return;
        }

        if (amount > long.MaxValue / Dimensions.ExpPerSpiritPurchase)
        {
            ctx.Send("That is more experience than exists.");
            return;
        }

        long granted = amount * Dimensions.ExpPerSpiritPurchase;
        long total = ctx.Player.Experience + ctx.Player.ExperienceSold;

        // Prospective, not current. AddExperience early-returns when the CURRENT total is
        // over the cap (Player.cs:1653-1660), so checking the same condition here only
        // catches players who are already past it - a player one experience under the cap
        // passes, buys, and lands 24,999,999 above a ceiling the server is meant to
        // enforce. Test what the purchase would produce.
        if (ctx.World.Settings.ExperienceCap > 0 && total + granted > ctx.World.Settings.ExperienceCap)
        {
            long affordable = (ctx.World.Settings.ExperienceCap - total) / Dimensions.ExpPerSpiritPurchase;
            ctx.Send(affordable > 0
                ? "That would carry you past the experience cap. You can buy at most " + affordable + "."
                : "You have reached the experience cap.");
            return;
        }

        var spirit = ctx.World.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long before = spirit.GetBalance(ctx.Player);
        if (before < amount)
        {
            ctx.Send("Not enough " + spirit.Name + ".");
            return;
        }

        spirit.Remove(ctx.Player, amount, ctx.World);
        ctx.Player.AddExperience(granted, ctx.World, Player.ExperienceMessage.Normal, applyModifiers: false);

        ctx.Send("You spend " + amount + " " + spirit.Name + " to gain " + granted.ToString("N0") + " experience.");
        ctx.World.LogHandler.Log(Log.Types.BuyExperience, ctx.Player,
            "BuyExperience: " + amount + " " + spirit.ShortName + " -> " + granted + " exp"
            + ", spirit " + before + " -> " + (before - amount));
    }

    public static void GiveSpirit(CommandContext ctx, Player target, long amount)
    {
        if (target.State != Player.States.Ready)
        {
            ctx.Send(target.Name + " is not online.");
            return;
        }

        if (amount <= 0)
        {
            ctx.Send("/givesp <player> <amount>");
            return;
        }

        if (target == ctx.Player)
        {
            ctx.Send("You cannot give spirit to yourself.");
            return;
        }

        var spirit = ctx.World.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long senderBefore = spirit.GetBalance(ctx.Player);
        if (senderBefore < amount)
        {
            ctx.Send("Not enough " + spirit.Name + ".");
            return;
        }

        // The recipient side, checked before either wallet moves. BaseStats.SP is a long,
        // so a transfer into a large enough wallet wraps negative; MaxSpiritBalance keeps
        // the refusal well short of that and makes a faucet bug visible as a refusal
        // rather than as a corrupted balance.
        long targetBefore = spirit.GetBalance(target);
        if (targetBefore > Dimensions.MaxSpiritBalance - amount)
        {
            ctx.Send(target.Name + " cannot hold that much " + spirit.Name + ".");
            return;
        }

        spirit.Remove(ctx.Player, amount, ctx.World);
        spirit.Add(target, amount, ctx.World);

        ctx.Send("You give " + amount + " " + spirit.Name + " to " + target.Name + ".");
        ctx.World.Send(target, P.ServerMessage(
            ctx.Player.Name + " gives you " + amount + " " + spirit.Name + "."));

        // One entry per side, each naming the counterparty in otherid and carrying its own
        // before/after. Two rows rather than one because logs are queried per player.
        ctx.World.LogHandler.Log(Log.Types.GiveSpirit, ctx.Player,
            "GiveSpirit: sent " + amount + " " + spirit.ShortName + " to " + target.Name
            + ", balance " + senderBefore + " -> " + (senderBefore - amount),
            target.PlayerID);
        ctx.World.LogHandler.Log(Log.Types.GiveSpirit, target,
            "GiveSpirit: received " + amount + " " + spirit.ShortName + " from " + ctx.Player.Name
            + ", balance " + targetBefore + " -> " + (targetBefore + amount),
            ctx.Player.PlayerID);
    }
}
