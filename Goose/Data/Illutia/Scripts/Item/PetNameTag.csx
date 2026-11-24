using Goose;
using Goose.Scripting;

public class PetNameTag : BaseItemScript
{
    public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
    {
        if (player.Pets.Count == 0)
        {
            world.Send(player, P.Notice(NoticeType.Info, "You have no pets to name."));
            return false;
        }

        foreach (var w in player.Windows.Where(w => w.Type == Window.WindowTypes.OptionList && w.NPC is null).ToList())
            w.Close(player, world);

        var ids = player.Pets.Select(p => p.PetID).ToList();
        var lines = player.Pets.Select(PetMenu.PetLine).ToList();
        new OptionListWindow(player, world, "Name Tag - choose a pet", lines, (index, p, w) =>
        {
            var pet = PetOperations.Find(p, ids[index]);
            if (pet is null)
            {
                w.Send(p, P.Notice(NoticeType.Info, "That pet is gone."));
                return;
            }

            TextInputWindow? input = null;
            input = new TextInputWindow(p, w, "Name Tag", $"New name for {pet.Name}:", "Rename",
                pet.Name, NameRules.MaxLength, (text, pp, ww) =>
                {
                    var target = PetOperations.Find(pp, ids[index]);
                    if (target is null)
                    {
                        ww.Send(pp, P.Notice(NoticeType.Info, "That pet is gone."));
                        input!.Close(pp, ww);
                        return;
                    }

                    if (!NameRules.IsValidPetName(text))
                    {
                        ww.Send(pp, P.Notice(NoticeType.Info, "Pet names must be 3-16 letters, with single spaces between words."));
                        return;
                    }

                    if (!pp.Inventory.TryConsumeItem(item, 1, ww))
                    {
                        ww.Send(pp, P.Notice(NoticeType.Info, "Your name tag is gone."));
                        input!.Close(pp, ww);
                        return;
                    }

                    var result = PetOperations.Rename(pp, target, text, ww);
                    ww.Send(pp, P.Notice(NoticeType.Info, result.Message));
                    input!.Close(pp, ww);
                });
        });

        return false;
    }
}

return typeof(PetNameTag);
