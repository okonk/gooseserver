using Goose;
using Goose.Scripting;

public class BankPageItem : BaseItemScript
{
    public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
    {
        if (!int.TryParse(item.ScriptParams, out int pages) || pages <= 0)
        {
            world.Send(player, P.ServerMessage("This item has no valid bank pages configured."));
            return false;
        }

        player.NumberOfBankPages += pages;

        foreach (var container in player.Bank.Containers.Values)
        {
            container.GrowToPages(player.NumberOfBankPages, world.Settings.BankSlotsPerPage);
        }

        foreach (var window in player.Windows)
        {
            if (window is BankWindow bankWindow)
                bankWindow.MaxPages = player.NumberOfBankPages;
        }

        world.Send(player, P.ServerMessage($"Your bank slots have been increased. You now have {player.NumberOfBankPages} pages."));
        return true;
    }
}

return typeof(BankPageItem);
