using System;
using Goose;
using Goose.Events;
using Goose.Scripting;

public class AsperetaMode : BaseGlobalScript
{
	public override void OnLoaded(GameWorld world)
	{
        // TODO: Custom command and hairdye command
        // TODO: Right click item info in vendors + inventory

        Console.WriteLine("Loading Aspereta Mode");

        BankWindow.IdGenerator = (player) => { return ++player.LastWindowID; };
        CombineBagWindow.IdGenerator = (player) => { return ++player.LastWindowID; };

        FacingEvent.FacingConverter = (facing) => { return (facing == 2 ? 3 : (facing == 3 ? 4 : (facing == 4 ? 2 : facing))); };

        P.ClassUpdate = (@class) => { return null; };

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
                    (spell.Target == Spell.SpellTargets.Target ? "T" : "X");
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
	}
}

return typeof(AsperetaMode);