using System;
using Goose;
using Goose.Events;
using Goose.Scripting;

public class ItemInfoWindow : Window
{
    private IItem item;

    public ItemInfoWindow(GameWorld world, Player player, IItem item)
    {
        this.ID = ++player.LastWindowID;
        this.Frame = WindowFrames.GenericInfo;
        this.Type = WindowTypes.ItemInfo;
        this.item = item;

        this.Title = item.Name;
        this.Buttons = "0,0,0,0,0";

        this.SendCreate(player, world);
    }

    public static void Open(GameWorld world, Player player, IItem item)
    {
        player.Windows.Add(new ItemInfoWindow(world, player, item));
    }

    public override void Populate(Player player, GameWorld world)
    {
        int lineno = 1;
        string line = "";
        if (item.IsLore) line += "LORE ";
        if (item.IsEvent) line += "EVENT ";
        if (item.IsBindOnPickup) line += "BOP ";
        if (item.IsBindOnEquip) line += "BOE ";
        if (item is Item && ((Item)item).IsBound) line += "BOUND ";
        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));

        world.Send(player, P.WindowTextLine(this.ID, lineno++, item.Description));

        if (item.UseType == ItemTemplate.UseTypes.Armor || item.UseType == ItemTemplate.UseTypes.Weapon)
        {
            if (item.Slot == ItemTemplate.ItemSlots.OneHanded ||
                item.Slot == ItemTemplate.ItemSlots.TwoHanded)
            {
                line = "DMG: " + item.WeaponDamage;
                line += " DLY: " + item.WeaponDelay;
                line += " " + item.Type.ToString();

                world.Send(player, P.WindowTextLine(this.ID, lineno++, line));
            }
            else
            {
                line = "Type: " + item.Type.ToString();
                world.Send(player, P.WindowTextLine(this.ID, lineno++, line));
            }
        }
        else
        {
            world.Send(player, P.WindowTextLine(this.ID, lineno++, ""));
        }

        AttributeSet stats = null;
        if (item is Item) stats = ((Item)item).TotalStats;
        else stats = item.BaseStats;

        line = "";
        if (stats.AC != 0) line += "AC: " + stats.AC + " ";
        if (stats.HP != 0) line += "HP: " + stats.HP + " ";
        if (stats.MP != 0) line += "MP: " + stats.MP + " ";
        if (stats.SP != 0) line += "SP: " + stats.SP + " ";
        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));

        line = "";
        if (stats.Strength != 0) line += "STR: " + stats.Strength + " ";
        if (stats.Stamina != 0) line += "STA: " + stats.Stamina + " ";
        if (stats.Intelligence != 0) line += "INT: " + stats.Intelligence + " ";
        if (stats.Dexterity != 0) line += "DEX: " + stats.Dexterity + " ";
        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));

        line = "";
        if (stats.FireResist != 0) line += "FR: " + stats.FireResist + " ";
        if (stats.AirResist != 0) line += "AR: " + stats.AirResist + " ";
        if (stats.EarthResist != 0) line += "ER: " + stats.EarthResist + " ";
        if (stats.WaterResist != 0) line += "WR: " + stats.WaterResist + " ";
        if (stats.SpiritResist != 0) line += "SR: " + stats.SpiritResist + " ";
        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));

        line = "";
        if (item.MinLevel >= 1) line += "Level " + item.MinLevel + " ";

        /* This part will probably need changing later */
        for (int i = 1; i <= world.ClassHandler.Count; i++)
        {
            Class c = world.ClassHandler.GetClass(i);

            if ((item.ClassRestrictions & (int)Math.Pow(2, c.ClassID)) == 0)
            {
                line += c.ClassName + " ";
            }
        }

        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));

        line = "";
        if (item.MinExperience > 0)
        {
            line += "Sold: " + item.MinExperience;
        }
        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));

        line = "";
        if (item.SpellEffect != null)
        {
            line += "Effect: " + item.SpellEffect.Name;
            line += " (" + (int)Math.Round(item.SpellEffectChance, 0) + "%)";
        }
        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));

        line = "";
        if (item.UseType == ItemTemplate.UseTypes.Armor || item.UseType == ItemTemplate.UseTypes.Weapon)
        {
            line += "Slot: " + item.Slot.ToString() + " ";
        }
        line += "Value: " + item.Value;
        if (item.Credits > 0) line += "g / " + item.Credits + "cr";
        world.Send(player, P.WindowTextLine(this.ID, lineno++, line));
    }

    public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
    {
        switch (buttonid)
        {
            case ButtonTypes.Exit:
            case ButtonTypes.Close:
                player.Windows.Remove(this);
                break;
            default:
                player.Windows.Remove(this);
                break;
        }
    }

    public override void Refresh(Player player, GameWorld world)
    {
        this.Populate(player, world);
    }
}

public class ItemInfoEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        Event e = new ItemInfoEvent();
        e.Player = player;
        e.Data = data;

        return e;
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State == Player.States.Ready)
        {
            int itemid = 0;

            string data = ((string)this.Data).Substring(3);

            // log bad id
            if (data.Length <= 0) return;

            try
            {
                itemid = Convert.ToInt32(data);
            }
            catch (Exception)
            {
                itemid = 0;
            }

            // log bad item id
            if (itemid <= 0) return;

            if (itemid >= GameWorld.Settings.ItemIDStartpoint)
            {
                foreach (ItemSlot slot in this.Player.Inventory.GetEquippedSlots())
                {
                    if (slot != null && slot.Item.ItemID == itemid)
                    {
                        ItemInfoWindow.Open(world, this.Player, slot.Item);
                        return;
                    }
                }

                foreach (ItemSlot slot in this.Player.Inventory.GetInventorySlots())
                {
                    if (slot != null && slot.Item.ItemID == itemid)
                    {
                        ItemInfoWindow.Open(world, this.Player, slot.Item);
                        return;
                    }
                }
            }
            else
            {
                foreach (Window window in this.Player.Windows)
                {
                    if (window.Type != Window.WindowTypes.Vendor) continue;

                    foreach (NPCVendorSlot slot in window.NPC.VendorItems)
                    {
                        if (slot == null) continue;

                        if (slot.ItemTemplate.ID == itemid)
                        {
                            ItemInfoWindow.Open(world, this.Player, slot.ItemTemplate);

                            return;
                        }
                    }
                }

                // if got here log bad GID packet since they have no open vendors with that template id
            }
        }
    }
}

public class AsperetaMode : BaseGlobalScript
{
	public override void OnLoaded(GameWorld world)
	{
        // TODO: Custom command and hairdye command

        Console.WriteLine("Loading Aspereta Mode");

        world.EventHandler.RegisterEvent("GID", ItemInfoEvent.Create);

        BankWindow.IdGenerator = (player) => { return ++player.LastWindowID; };
        CombineBagWindow.IdGenerator = (player) => { return ++player.LastWindowID; };

        FacingEvent.FacingConverter = (facing) => { return (facing == 2 ? 3 : (facing == 3 ? 4 : (facing == 4 ? 2 : facing))); };

        P.ClassUpdate = (@class) => { return null; };

        P.WindowTextLine = (windowId, lineNo, line) =>
        {
            return string.Format("WNF{0},{1},{2}|0|0|0|*", windowId, lineNo, line);
        };

        P.Emote = (player, data) =>
        {
            const int MAX_EMOTES = 12;

            int emot = 0;

            try
            {
                emot = Convert.ToInt32(data);
            }
            catch (Exception)
            {
                emot = 0;
            }

            if (emot <= 0 || emot > MAX_EMOTES) return null;

            return "EMOT" + player.LoginID + "," + emot;
        };

        P.MakeObject = (tile) =>
        {
            string rgba = "*";
            if (tile.ItemSlot.Item.GraphicA != 0)
            {
                rgba = tile.ItemSlot.Item.GraphicR + "," +
                tile.ItemSlot.Item.GraphicG + "," +
                tile.ItemSlot.Item.GraphicB + "," +
                tile.ItemSlot.Item.GraphicA;
            }

            return "MOB" +
                   tile.ItemSlot.Item.GraphicTile + "," +
                   tile.X + "," +
                   tile.Y + "," +
                   tile.ItemSlot.Item.Name + (tile.ItemSlot.Item.IsBindOnPickup ? " (BoP)" : "") + "," +
                   tile.ItemSlot.Stack + "," +
                   rgba;
        };

		P.SendCurrentMap = (map) =>
        {
            return "SCM" + map.FileName.Replace("Map", "").Replace(".map", "") + ",1," + map.Name;
        };

        P.MakeCharacter = (player) =>
        {
            if (player is Pet) return P.MakePetCharacter((Pet)player);

            int pose = player.BodyState;
            ItemSlot weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon != null)
            {
                pose = weapon.Item.BodyState;
            }

            return "MKC" + player.LoginID + "," +
                           "1," +
                           player.Name + "," +
                           player.Title + "," +
                           player.Surname + "," +
                           "" + "," + // Guild name
                           player.MapX + "," +
                           player.MapY + "," +
                           player.Facing + "," +
                           (int)(((float)player.CurrentHP / player.MaxStats.HP) * 100) + "," + // HP %
                           player.CurrentBodyID + "," +
                           (player.CurrentBodyID >= 100 ? 1 : pose) + "," +
                           (player.CurrentBodyID >= 100 ? 0 : player.HairID) + "," +
                           player.Inventory.EquippedDisplay() + // Note: EquippedDisplay() adds it's own , on end
                           player.HairR + "," +
                           player.HairG + "," +
                           player.HairB + "," +
                           player.HairA + "," +
                           "0" + "," + // Invis thing
                           (player.CurrentBodyID >= 100 ? 0 : player.FaceID);
        };

        P.UpdateCharacter = (player) =>
        {
            if (player is Pet) return P.UpdatePet((Pet)player);

            int pose = player.BodyState;
            ItemSlot weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon != null)
            {
                pose = weapon.Item.BodyState;
            }

            return "CHP" +
                    player.LoginID + "," +
                    player.CurrentBodyID + "," +
                    (player.CurrentBodyID >= 100 ? 1 : pose) + "," +
                    (player.CurrentBodyID >= 100 ? 0 : player.HairID) + "," +
                    player.Inventory.EquippedDisplay() + // Note: EquippedDisplay() adds it's own , on end
                    player.HairR + "," +
                    player.HairG + "," +
                    player.HairB + "," +
                    player.HairA + "," +
                    "0" + "," + // Invis thing
                    (player.CurrentBodyID >= 100 ? 0 : player.FaceID);
        };

        P.MakeNPCCharacter = (npc) =>
        {
            return "MKC" + npc.LoginID + ",2," +
                           npc.Name + "," +
                           npc.Title + "," +
                           npc.Surname + "," +
                           "" + "," + // Guild name
                           npc.MapX + "," +
                           npc.MapY + "," +
                           npc.Facing + "," +
                           (int)(((float)npc.CurrentHP / npc.MaxStats.HP) * 100) + "," + // HP %
                           npc.CurrentBodyID + "," +
                           (npc.CurrentBodyID >= 100 ? 1 : npc.BodyState) + "," +
                           (npc.CurrentBodyID >= 100 ? 0 : npc.HairID) + "," +
                           npc.EquippedItems + "," +
                           npc.HairR + "," +
                           npc.HairG + "," +
                           npc.HairB + "," +
                           npc.HairA + "," +
                           "0" + "," + // Invis thing
                           (npc.CurrentBodyID >= 100 ? 0 : npc.FaceID);
        };

        P.UpdateNPC = (npc) =>
        {
            return "CHP" +
                    npc.LoginID + "," +
                    npc.CurrentBodyID + "," +
                    (npc.CurrentBodyID >= 100 ? 1 : npc.BodyState) + "," +
                    (npc.CurrentBodyID >= 100 ? 0 : npc.HairID) + "," +
                    npc.EquippedItems + "," +
                    npc.HairR + "," +
                    npc.HairG + "," +
                    npc.HairB + "," +
                    npc.HairA + "," +
                    "0" + "," + // Invis thing
                    (npc.CurrentBodyID >= 100 ? 0 : npc.FaceID);
        };

        P.MakePetCharacter = (npc) =>
        {
            return "MKC" + npc.LoginID + ",2," +
                           npc.Name + "," +
                           npc.Title + "," +
                           npc.Surname + "," +
                           "" + "," + // Guild name
                           npc.MapX + "," +
                           npc.MapY + "," +
                           npc.Facing + "," +
                           (int)(((float)npc.CurrentHP / npc.MaxStats.HP) * 100) + "," + // HP %
                           npc.CurrentBodyID + "," +
                           (npc.CurrentBodyID >= 100 ? 1 : npc.BodyState) + "," +
                           (npc.CurrentBodyID >= 100 ? 0 : npc.HairID) + "," +
                           npc.EquippedItems + "," +
                           npc.HairR + "," +
                           npc.HairG + "," +
                           npc.HairB + "," +
                           npc.HairA + "," +
                           "0" + "," + // Invis thing
                           (npc.CurrentBodyID >= 100 ? 0 : npc.FaceID);
        };

        P.UpdatePet = (npc) =>
        {
            return "CHP" +
                    npc.LoginID + "," +
                    npc.CurrentBodyID + "," +
                    (npc.CurrentBodyID >= 100 ? 1 : npc.BodyState) + "," +
                    (npc.CurrentBodyID >= 100 ? 0 : npc.HairID) + "," +
                    npc.EquippedItems + "," +
                    npc.HairR + "," +
                    npc.HairG + "," +
                    npc.HairB + "," +
                    npc.HairA + "," +
                    "0" + "," + // Invis thing
                    (npc.CurrentBodyID >= 100 ? 0 : npc.FaceID);
        };

        P.InventorySlot = (item, world, slotId, stack) =>
        {
            return "SIS" + P.ItemSlot(item, world, slotId, stack);
        };

        P.ClearInventorySlot = (slotId) =>
        {
            return "SIS" + slotId;
        };

        P.ItemSlot = (item, world, slotId, stack) =>
        {
            return 
                slotId + "," + 
                item.ItemID + "," + 
                item.Name + "," +
                stack + "," + 
                item.GraphicTile + "," +
                item.GraphicR + "," + 
                item.GraphicG + "," +
                item.GraphicB + "," + 
                item.GraphicA;
        };

        P.EquipSlot = (item, world, slotId, stack) =>
        {
            return "WNF11," + slotId + "," + 
                item.Name + "|" +
                stack + "|" + 
                item.ItemID + "|" + 
                item.GraphicTile + "|" +
                item.GraphicR + "|" + 
                item.GraphicG + "|" +
                item.GraphicB + "|" + 
                item.GraphicA;
        };

        P.ClearEquipSlot = (slotId) =>
        {
            return "WNF11," + slotId + ", |0|0|0|*";
        };

        P.BankSlot = (window, item, world, slotId, stack) =>
        {
            return "WNF" + window.ID + "," + slotId + "," + 
                item.Name + "|" +
                stack + "|" + 
                item.ItemID + "|" + 
                item.GraphicTile + "|" +
                item.GraphicR + "|" + 
                item.GraphicG + "|" +
                item.GraphicB + "|" + 
                item.GraphicA;
        };

        P.ClearBankSlot = (window, slotId) =>
        {
            return "WNF" + window.ID + "," + slotId + ", |0|0|0|*";
        };

        P.CombineSlot = P.BankSlot;

        P.ClearCombineSlot = P.ClearBankSlot;

        P.VendorSlot = (window, item, world, slotId, stack) =>
        {
            return "WNF" + window.ID + "," + slotId + "," + item.Name +
                            (stack > 1 ? " (" + stack + ")" : "") + "|" + 0 + "|" +
                            item.ID + "|" + item.GraphicTile + "|*";
        };

        P.ClearVendor = () =>
        {
            return null;
        };

        P.VitalsPercentage = (target) =>
        {
            return "VC" + target.LoginID + "," +
                   (int)(((float)target.CurrentHP / target.MaxHP) * 100) + "," +
                   (int)(((float)target.CurrentMP / target.MaxMP) * 100);
        };

        P.SpellSlot = (spell, slotId, targetType) =>
        {
            if (spell == null)
            {
                return "SSS" + slotId;
            }

            return "SSS" + slotId + "," +
                    spell.Name + "," +
                    "0," +
                    "0," +
                    (spell.Target == Spell.SpellTargets.Target ? "T" : "X") + "," +
                    spell.Graphic + "," +
                    spell.GraphicFile;
        };

        P.SpellPlayer = (loginId, animationId, animationFile) => 
        { 
            return "SPP" + loginId + "," + animationId; 
        };

        P.SpellTile = (x, y, animationId, animationFile) => 
        { 
            return "SPA" +
                x + "," +
                y + "," +
                animationId;
        };

        P.Tell = (player, message) => { return "$6[tell from] " + player.Name + ": " + message; };

        P.NPCAngryEmote = (target) =>
        {
            return "EMOT" + target.LoginID + ",8";
        };

        P.BuffBar = (buff, index) => 
        {
            if (buff == null)
            {
                return "BUF" + index;
            }

            return "BUF" + index + "," + buff.SpellEffect.BuffGraphic + "," + buff.SpellEffect.Name; 
        };

        P.ExpBar = (player) => 
        {
            long percent, tnl, exp;
            bool xpCapped = false;
            if (player.Class.GetLevel(player.Level).Experience == 0)
            {
                percent = 100;
                tnl = exp = Math.Min(player.Experience, int.MaxValue - 1);
                xpCapped = player.Experience >= int.MaxValue;
            }
            else
            {
                long prev;
                if (player.Level == 1) prev = 0;
                else prev = player.Class.GetLevel(player.Level - 1).Experience;

                long next = player.Class.GetLevel(player.Level).Experience;
                tnl = next - player.Experience;
                exp = player.Experience;
                percent = (long)(((float)(exp - prev) / (next - prev)) * 100);
            }

            string packet = "TNL" + percent + "," + exp + "," + tnl;
            if (xpCapped)
            {
                packet += "," + player.Experience;
            }

            return packet;
        };

        P.Cast = (target) =>
        {
            return "CST" + target.LoginID;
        };
	}
}

return typeof(AsperetaMode);