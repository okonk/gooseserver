namespace Goose.Commands
{
    [Command("/hairdye", Section = "General", Help = "Buy hair dye potions of any colour.")]
    public sealed class HairdyeCommand : BaseCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private const int PotionsPerPurchase = 5;

        [Subcommand("preview", Help = "Preview a hair colour on the map.")]
        public void Preview(CommandContext ctx, int r, int g, int b, int a)
        {
            var world = ctx.World;

            string? error = ValidateRGBA(r, g, b, a);
            if (error is not null)
            {
                ctx.Send(error);
                return;
            }

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

            if (world.Settings.ServerType == "Illutia")
            {
                world.Send(ctx.Player,
                    "MKC" + 9000 + "," +
                    "1," +
                    "Hairdye Preview," +
                    "," +
                    "," +
                    "" + "," + // Guild name
                    prevx + "," +
                    prevy + "," +
                    (int)ctx.Player.Facing + "," +
                    100 + "," + // HP %
                    ctx.Player.BodyID + "," +
                    ctx.Player.BodyR + "," + // Body Color R
                    ctx.Player.BodyG + "," + // Body Color G
                    ctx.Player.BodyB + "," + // Body Color B
                    ctx.Player.BodyA + "," + // Body Color A
                    pose + "," +
                    ctx.Player.HairID + "," +
                    ctx.Player.Inventory.EquippedDisplay() + // Note: EquippedDisplay() adds it's own , on end
                    r + "," +
                    g + "," +
                    b + "," +
                    a + "," +
                    "0" + "," + // Invis thing
                    ctx.Player.FaceID + "," +
                    ctx.Player.CalculateMoveSpeed() + "," + // Move Speed
                    "0" + "," + // Player Name Color
                    ctx.Player.Inventory.MountDisplay()); // Mount
            }
            else
            {
                world.Send(ctx.Player,
                    "MKC" + 9000 + "," +
                    "1," +
                    "Hairdye Preview," +
                    "," +
                    "," +
                    "" + "," + // Guild name
                    prevx + "," +
                    prevy + "," +
                    (int)ctx.Player.Facing + "," +
                    100 + "," + // HP %
                    ctx.Player.BodyID + "," +
                    ctx.Player.BodyR + "," + // Body Color R
                    ctx.Player.BodyG + "," + // Body Color G
                    ctx.Player.BodyB + "," + // Body Color B
                    ctx.Player.BodyA + "," + // Body Color A
                    pose + "," +
                    ctx.Player.HairID + "," +
                    ctx.Player.Inventory.EquippedDisplay() + // Note: EquippedDisplay() adds it's own , on end
                    r + "," +
                    g + "," +
                    b + "," +
                    a + "," +
                    "0" + "," + // Invis thing
                    ctx.Player.FaceID);
            }
        }

        [Subcommand("kill", Help = "Remove the hair colour preview from the map.")]
        public void Kill(CommandContext ctx)
        {
            ctx.World.Send(ctx.Player, P.EraseCharacter(9000));
        }

        [Subcommand("create", Help = "Buy five hair dye potions in a colour of your choosing.",
            Usage = "/hairdye create <r> <g> <b> <a> <name...>")]
        public void Create(CommandContext ctx, int r, int g, int b, int a, string[] name)
        {
            var world = ctx.World;

            if (name.Length == 0)
            {
                ctx.Send(ctx.Usage);
                return;
            }

            string? error = ValidateRGBA(r, g, b, a);
            if (error is not null)
            {
                ctx.Send(error);
                return;
            }

            if (ctx.Player.Gold < world.Settings.HairdyeCommandCost)
            {
                ctx.Send($"/hairdye create requires {world.Settings.HairdyeCommandCost} gold.");
                return;
            }

            // The name is written into packets with comma- and pipe-delimited fields.
            string? itemName = CustomItem.SanitizeName(string.Join(" ", name));
            if (itemName is null)
            {
                ctx.Send(ctx.Usage);
                return;
            }

            ItemTemplate? template = world.ItemHandler.GetTemplate(world.Settings.HairDyePotionId);
            if (template is null)
            {
                log.Error("hair dye potion template {0} is missing; /hairdye create refused",
                    world.Settings.HairDyePotionId);
                return;
            }

            Item potion = new Item();
            if (!potion.LoadFromTemplate(template))
            {
                log.Error("hair dye potion template {0}: invalid template; /hairdye create refused", template.ID);
                return;
            }

            if (potion.StackSize != 0 && potion.StackSize < PotionsPerPurchase)
            {
                log.Error("hair dye potion template {0}: stack size {1} is below purchase quantity {2}; /hairdye create refused",
                    template.ID, potion.StackSize, PotionsPerPurchase);
                return;
            }

            potion.Name = itemName;
            // The prefix is load-bearing: Item.Custom keys off it (Item.cs:129), and
            // DestroyItemEvent reads that flag.
            potion.Description = "Custom created by " + ctx.Player.Name;
            potion.GraphicR = r;
            potion.GraphicG = g;
            potion.GraphicB = b;
            potion.GraphicA = a;
            potion.ScriptParams = $"{r},{g},{b},{a}";

            int slot = FindPotionSlot(world, ctx.Player, potion);
            if (slot == 0)
            {
                ctx.Send("You don't have enough inventory space for the potions.");
                return;
            }

            ItemSlot? target = ctx.Player.Inventory.GetSlot(slot);
            if (target is null)
            {
                world.ItemHandler.AddAndAssignId(potion, world);
                ctx.Player.Inventory.SetSlot(slot, new ItemSlot { Item = potion, Stack = PotionsPerPurchase });
            }
            else
            {
                target.Stack += PotionsPerPurchase;
            }

            ctx.Player.Inventory.SendSlot(slot, world);
            ctx.Player.RemoveGold(world.Settings.HairdyeCommandCost, world);

            ctx.Send($"Bought {PotionsPerPurchase} {itemName} for {world.Settings.HairdyeCommandCost} gold.");
        }

        /// <summary>Returns the 1-based slot the potions go in, or 0 when the inventory can
        /// take neither a new stack nor five more of an identical one.</summary>
        private static int FindPotionSlot(GameWorld world, Player player, Item potion)
        {
            var inventory = player.Inventory;
            var purchase = new ItemSlot { Item = potion, Stack = PotionsPerPurchase };
            int free = 0;

            for (int i = 1; i <= world.Settings.InventorySize; i++)
            {
                ItemSlot? slot = inventory.GetSlot(i);

                if (slot is null)
                {
                    if (free == 0) free = i;
                    continue;
                }

                if (slot.CanStack(purchase)) return i;
            }

            return free;
        }

        private static string? ValidateRGBA(int r, int g, int b, int a)
        {
            if (r < 0 || r > 255) return "/hairdye: invalid r value";
            if (g < 0 || g > 255) return "/hairdye: invalid g value";
            if (b < 0 || b > 255) return "/hairdye: invalid b value";
            if (a < 0 || a > 255) return "/hairdye: invalid a value";

            return null;
        }
    }
}
