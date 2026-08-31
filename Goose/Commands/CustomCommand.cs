using static Goose.Inventory;

namespace Goose.Commands
{
    [Command("/custom", Section = "Customs", Help = "Customise an item's colour and look using a custom ticket.")]
    public sealed class CustomCommand : BaseCommand
    {
        [Subcommand("help", Help = "Show the custom instructions.")]
        public void Help(CommandContext ctx)
        {
            var world = ctx.World;

            // Duplicated per handler: the framework answers bare/unknown subcommands before any handler runs.
            var combineBag = ctx.Player.Inventory.GetCombineBagContainer();
            var firstSlot = combineBag.GetSlot(1);
            if (firstSlot is null || firstSlot.Item.TemplateID != world.Settings.CustomTicketId)
            {
                ctx.Send("You need a custom ticket in your first combine bag slot to use this command.");
                return;
            }

            ctx.Send("In combine bag: Place custom ticket in first slot. Place the item you want the stats of in second slot. Place the item you want the look of in the third slot.");
            ctx.Send("Type /custom preview <r> <g> <b> <a> <custom name> to preview the colour and look");
            ctx.Send("Type /custom make <r> <g> <b> <a> <custom name> to make the custom. It will destroy your custom ticket and source items.");
        }

        [Subcommand("kill", Help = "Remove the custom preview from the map.")]
        public void Kill(CommandContext ctx)
        {
            var world = ctx.World;

            // Duplicated per handler: the framework answers bare/unknown subcommands before any handler runs.
            var combineBag = ctx.Player.Inventory.GetCombineBagContainer();
            var firstSlot = combineBag.GetSlot(1);
            if (firstSlot is null || firstSlot.Item.TemplateID != world.Settings.CustomTicketId)
            {
                ctx.Send("You need a custom ticket in your first combine bag slot to use this command.");
                return;
            }

            world.Send(ctx.Player, P.EraseCharacter(9000));
        }

        [Subcommand("preview", Help = "Preview the custom's colour and look.", Usage = "/custom preview <r> <g> <b> <a> <name...>")]
        public void Preview(CommandContext ctx, int r, int g, int b, int a, string[] name)
        {
            var world = ctx.World;

            // Duplicated per handler: the framework answers bare/unknown subcommands before any handler runs.
            var combineBag = ctx.Player.Inventory.GetCombineBagContainer();
            var firstSlot = combineBag.GetSlot(1);
            if (firstSlot is null || firstSlot.Item.TemplateID != world.Settings.CustomTicketId)
            {
                ctx.Send("You need a custom ticket in your first combine bag slot to use this command.");
                return;
            }

            if (name.Length == 0)
            {
                ctx.Send(ctx.Usage);
                return;
            }

            string? error = ParseRGBA(r, g, b, a);
            if (error is not null)
            {
                ctx.Send(error);
                return;
            }

            if (!ValidateCustomSlots(world, combineBag, ctx.Player))
                return;

            ItemSlot lookSlot = combineBag.GetSlot(3)!;

            int prevx = ctx.Player.MapX;
            int prevy = ctx.Player.MapY;

            if (prevx == 1) prevx += 1;
            else prevx -= 1;

            int pose = ctx.Player.BodyState;
            ItemSlot? weapon = ctx.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon is not null)
            {
                pose = weapon.Item.BodyState;
            }

            if (lookSlot.Item.Slot == ItemTemplate.ItemSlots.OneHanded || lookSlot.Item.Slot == ItemTemplate.ItemSlots.TwoHanded)
            {
                pose = lookSlot.Item.BodyState;
            }

            if (world.Settings.ServerType == "Illutia")
            {
                world.Send(ctx.Player,
                    "MKC" + 9000 + "," +
                    "1," +
                    "Custom Preview," +
                    "," +
                    "," +
                    "" + "," + // Guild name
                    prevx + "," +
                    prevy + "," +
                    ctx.Player.Facing + "," +
                    100 + "," + // HP %
                    ctx.Player.BodyID + "," +
                    ctx.Player.BodyR + "," + // Body Color R
                    ctx.Player.BodyG + "," + // Body Color G
                    ctx.Player.BodyB + "," + // Body Color B
                    ctx.Player.BodyA + "," + // Body Color A
                    pose + "," +
                    ctx.Player.HairID + "," +
                    this.EquippedDisplay(ctx.Player, lookSlot, r, g, b, a) + // Note: EquippedDisplay() adds it's own , on end
                    ctx.Player.HairR + "," +
                    ctx.Player.HairG + "," +
                    ctx.Player.HairB + "," +
                    ctx.Player.HairA + "," +
                    "0" + "," + // Invis thing
                    ctx.Player.FaceID + "," +
                    ctx.Player.CalculateMoveSpeed() + "," + // Move Speed
                    "0" + "," + // Player Name Color
                    this.MountDisplay(ctx.Player, lookSlot, r, g, b, a)); // Mount
            }
            else
            {
                world.Send(ctx.Player,
                    "MKC" + 9000 + "," +
                    "1," +
                    "Custom Preview," +
                    "," +
                    "," +
                    "" + "," + // Guild name
                    prevx + "," +
                    prevy + "," +
                    ctx.Player.Facing + "," +
                    100 + "," + // HP %
                    ctx.Player.BodyID + "," +
                    pose + "," +
                    ctx.Player.HairID + "," +
                    this.EquippedDisplay(ctx.Player, lookSlot, r, g, b, a) + // Note: EquippedDisplay() adds it's own , on end
                    ctx.Player.HairR + "," +
                    ctx.Player.HairG + "," +
                    ctx.Player.HairB + "," +
                    ctx.Player.HairA + "," +
                    "0" + "," + // Invis thing
                    ctx.Player.FaceID);
            }
        }

        [Subcommand("make", "create", Help = "Create the custom. Destroys the custom ticket and source items.", Usage = "/custom make <r> <g> <b> <a> <name...>")]
        public void Make(CommandContext ctx, int r, int g, int b, int a, string[] name)
        {
            var world = ctx.World;

            // Duplicated per handler: the framework answers bare/unknown subcommands before any handler runs.
            var combineBag = ctx.Player.Inventory.GetCombineBagContainer();
            var firstSlot = combineBag.GetSlot(1);
            if (firstSlot is null || firstSlot.Item.TemplateID != world.Settings.CustomTicketId)
            {
                ctx.Send("You need a custom ticket in your first combine bag slot to use this command.");
                return;
            }

            if (name.Length == 0)
            {
                ctx.Send(ctx.Usage);
                return;
            }

            string? error = ParseRGBA(r, g, b, a);
            if (error is not null)
            {
                ctx.Send(error);
                return;
            }

            if (!ValidateCustomSlots(world, combineBag, ctx.Player))
                return;

            ItemSlot ticketSlot = combineBag.GetSlot(1)!;
            ItemSlot statsSlot = combineBag.GetSlot(2)!;
            ItemSlot lookSlot = combineBag.GetSlot(3)!;

            Item item = new Item();
            if (!item.LoadFromTemplate(statsSlot.Item.Template)) return;
            item.StatMultiplier = statsSlot.Item.StatMultiplier;
            item.BaseStats = statsSlot.Item.BaseStats.Clone();
            item.TotalStats = statsSlot.Item.TotalStats.Clone();
            item.TotalWeaponDamage = statsSlot.Item.TotalWeaponDamage;
            item.BodyState = lookSlot.Item.BodyState;
            item.GraphicEquipped = lookSlot.Item.GraphicEquipped;
            item.GraphicR = r;
            item.GraphicG = g;
            item.GraphicB = b;
            item.GraphicA = a;
            item.GraphicTile = lookSlot.Item.GraphicTile;
            item.GraphicFile = lookSlot.Item.GraphicFile;

            string nameText = string.Join(" ", name);
            item.Name = (nameText.Length > 255 ? nameText.Substring(0, 255) : nameText).Replace(",", "");
            item.Description = "Custom created by " + ctx.Player.Name;
            item.IsBound = statsSlot.Item.IsBound;
            item.ScriptParams = statsSlot.Item.ScriptParams;

            if (statsSlot.Item.ItemProperties.TryGetValue(ItemProperty.TitleId, out object? titleId))
                item.ItemProperties[ItemProperty.TitleId] = titleId;

            if (statsSlot.Item.ItemProperties.TryGetValue(ItemProperty.SurnameId, out object? surnameId))
                item.ItemProperties[ItemProperty.SurnameId] = surnameId;

            world.ItemHandler.AddAndAssignId(item, world);

            long newTicketStack = ticketSlot.Stack - 1;
            if (newTicketStack <= 0)
            {
                ticketSlot.Item = item;
                ticketSlot.Stack = 1;

                combineBag.SetSlot(2, null);
            }
            else
            {
                ticketSlot.Stack = newTicketStack;
                statsSlot.Item = item;
                statsSlot.Stack = 1;
            }

            combineBag.SetSlot(3, null);

            var combineBagWindow = ctx.Player.Windows.FirstOrDefault(w => w.Type == Window.WindowTypes.CombineBag);
            if (combineBagWindow is not null) combineBagWindow.Refresh(ctx.Player, world);

            world.LogHandler.Log(Log.Types.CreatedCustom, ctx.Player,
                $"{item.Name} ({item.TemplateID}) {lookSlot.Item.TemplateID}|{r},{g},{b},{a}", item.ItemID);
        }

        public string EquippedDisplay(Player player, ItemSlot customLook, int r, int g, int b, int a)
        {
            string e = "";
            EquipSlots[] slots = new EquipSlots[]{EquipSlots.Chest, EquipSlots.Head,
                EquipSlots.Legs, EquipSlots.Feet, EquipSlots.Shield, EquipSlots.Weapon};
            ItemSlot? item;

            EquipSlots lookSlot = player.Inventory.ItemSlotToEquipSlot(customLook.Item.Slot);

            foreach (var eq in slots)
            {
                if (eq == lookSlot)
                {
                    e += customLook.Item.GraphicEquipped + "," +
                                 r + "," +
                                 g + "," +
                                 b + "," +
                                 a + ",";
                }
                else
                {
                    item = player.Inventory.GetEquippedSlot(eq);
                    if (item is not null)
                    {
                        if (item.Item.GraphicA == 0)
                        {
                            e += item.Item.GraphicEquipped + ",*,";
                        }
                        else
                        {
                            e += item.Item.GraphicEquipped + "," +
                                 item.Item.GraphicR + "," +
                                 item.Item.GraphicG + "," +
                                 item.Item.GraphicB + "," +
                                 item.Item.GraphicA + ",";
                        }
                    }
                    else
                    {
                        e += "0,*,";
                    }
                }
            }

            return e;
        }

        public string MountDisplay(Player player, ItemSlot customLook, int r, int g, int b, int a)
        {
            EquipSlots lookSlot = player.Inventory.ItemSlotToEquipSlot(customLook.Item.Slot);

            ItemSlot? item = player.Inventory.GetEquippedSlot(EquipSlots.Mount);
            string e = "";
            if (lookSlot == EquipSlots.Mount)
            {
                e += customLook.Item.GraphicEquipped + "," +
                             r + "," +
                             g + "," +
                             b + "," +
                             a + ",";
            }
            else if (item is not null)
            {
                if (item.Item.GraphicA == 0)
                {
                    e += item.Item.GraphicEquipped + ",*";
                }
                else
                {
                    e += item.Item.GraphicEquipped + "," +
                            item.Item.GraphicR + "," +
                            item.Item.GraphicG + "," +
                            item.Item.GraphicB + "," +
                            item.Item.GraphicA + ",";
                }
            }
            else
            {
                e += "0,*";
            }

            return e;
        }

        public static bool ValidateCustomSlots(GameWorld world, ItemContainer combineBag, Player player)
        {
            var statsSlot = combineBag.GetSlot(2);
            var lookSlot = combineBag.GetSlot(3);
            if (statsSlot is null || lookSlot is null)
            {
                world.Send(player, P.ServerMessage("Items missing for customisation"));
                world.Send(player, P.ServerMessage("In combine bag: Place custom ticket in first slot. Place the item you want the stats of in second slot. Place the item you want the look of in the third slot."));
                return false;
            }

            if ((statsSlot.Item.UseType != ItemTemplate.UseTypes.Armor &&
                 statsSlot.Item.UseType != ItemTemplate.UseTypes.Weapon)
                || (lookSlot.Item.UseType != ItemTemplate.UseTypes.Armor &&
                    lookSlot.Item.UseType != ItemTemplate.UseTypes.Weapon)
                || statsSlot.Item.Slot == ItemTemplate.ItemSlots.Ring
                || statsSlot.Item.Slot == ItemTemplate.ItemSlots.Necklace
                || statsSlot.Item.Slot == ItemTemplate.ItemSlots.Pauldrons
                || statsSlot.Item.Slot == ItemTemplate.ItemSlots.Cloak
                || statsSlot.Item.Slot == ItemTemplate.ItemSlots.Belt
                || statsSlot.Item.Slot == ItemTemplate.ItemSlots.Gloves)
            {
                world.Send(player, P.ServerMessage("Items to be customised must be equipment and must be visible items."));
                return false;
            }

            if ((statsSlot.Item.Slot == ItemTemplate.ItemSlots.OneHanded || statsSlot.Item.Slot == ItemTemplate.ItemSlots.TwoHanded)
                && (lookSlot.Item.Slot == ItemTemplate.ItemSlots.OneHanded || lookSlot.Item.Slot == ItemTemplate.ItemSlots.TwoHanded))
            {
                return true;
            }

            if (statsSlot.Item.Slot != lookSlot.Item.Slot)
            {
                world.Send(player, P.ServerMessage("Items to be customised must be of the same equipment type."));
                return false;
            }

            return true;
        }

        public static string? ParseRGBA(int r, int g, int b, int a)
        {
            if (r < 0 || r > 255) return "/custom: invalid r value";
            if (g < 0 || g > 255) return "/custom: invalid g value";
            if (b < 0 || b > 255) return "/custom: invalid b value";
            if (a < 0 || a > 255) return "/custom: invalid a value";

            return null;
        }
    }
}
