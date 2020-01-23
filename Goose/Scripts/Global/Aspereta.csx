using System;
using Goose;
using Goose.Scripting;

public class AsperetaMode : BaseGlobalScript
{
	public override void OnLoaded(GameWorld world)
	{
        // TODO: Set player bodystate to 1 initially
        // TODO: Emote event needs to be fixed
        // TODO: Facing event needs to be fixed
        // TODO: Bank needs to be fixed
        // TODO: NPCs/Pets
        // TODO: Map loading server side
        // TODO: Sending unneeded CUP packets

        Console.WriteLine("Loading Aspereta Mode");

		P.SendCurrentMap = (map) =>
        {
            return "SCM" + map.FileName.Replace("Map", "").Replace(".map", "") + ",1," + map.Name;
        };

        P.MakeCharacter = (player) =>
        {
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
            //if (player is Pet) return UpdatePet((Pet)player);

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

        P.InventorySlot = (item, world, slotId, stack) =>
        {
            return "SIS" + P.ItemSlot(item, world, slotId, stack);
        };

        P.ClearInventorySlot = (slotId) =>
        {
            return "CIS" + slotId;
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
            return "WNF11," + P.ItemSlot(item, world, slotId, stack);
        };

        P.ClearEquipSlot = (slotId) =>
        {
            return "WNF11," + slotId + ", |0|0|0|*";
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