﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Goose.Inventory;

namespace Goose.Events
{
    class CustomCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new CustomCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                var combineBag = this.Player.Inventory.GetCombineBagContainer();

                var firstSlot = combineBag.GetSlot(1);
                if (firstSlot == null || firstSlot.Item.TemplateID != GameSettings.Default.CustomTicketId)
                {
                    world.Send(this.Player, "$7You need a custom ticket in your first combine bag slot to use this command.");
                    return;
                }

                string data = ((string)this.Data).Substring(7);
                if (string.IsNullOrWhiteSpace(data))
                {
                    world.Send(this.Player, "$7/custom [help|preview|kill|make] <r> <g> <b> <a> <custom name>");
                    return;
                }

                string[] tokens = data.Split(new[] { ' ' }, 6, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < 6 && tokens[0].ToLower() != "help" && tokens[0].ToLower() != "kill")
                {
                    world.Send(this.Player, "$7/custom [help|preview|kill|make] <r> <g> <b> <a> <custom name>");
                    return;
                }

                int r, g, b, a;
                string error;
                ItemSlot lookSlot;
                ItemSlot statsSlot;

                switch (tokens[0].ToLower())
                {
                    case "help":
                        world.Send(this.Player, "$7In combine bag: Place custom ticket in first slot. Place the item you want the stats of in second slot. Place the item you want the look of in the third slot.");
                        world.Send(this.Player, "$7Type /custom preview <r> <g> <b> <a> <custom name> to preview the colour and look");
                        world.Send(this.Player, "$7Type /custom make <r> <g> <b> <a> <custom name> to make the custom. It will destroy your custom ticket and source items.");

                        break;
                    case "make":
                        error = ParseRGBA(tokens, out r, out g, out b, out a);
                        if (error != null)
                        {
                            world.Send(this.Player, error);
                            return;
                        }

                        if (!ValidateCustomSlots(world, combineBag, this.Player))
                            return;

                        ItemSlot ticketSlot = combineBag.GetSlot(1);
                        statsSlot = combineBag.GetSlot(2);
                        lookSlot = combineBag.GetSlot(3);

                        Item item = new Item();
                        item.LoadFromTemplate(statsSlot.Item.Template);
                        item.BodyState = lookSlot.Item.BodyState;
                        item.GraphicEquipped = lookSlot.Item.GraphicEquipped;
                        item.GraphicR = r;
                        item.GraphicG = g;
                        item.GraphicB = b;
                        item.GraphicA = a;
                        item.GraphicTile = lookSlot.Item.GraphicTile;
                        item.GraphicFile = lookSlot.Item.GraphicFile;
                        item.Name = (tokens[5].Length > 50 ? tokens[5].Substring(0, 50) : tokens[5]).Replace(",", "");
                        item.Description = "Custom created by " + this.Player.Name;
                        item.IsBound = statsSlot.Item.IsBound;
                        world.ItemHandler.AddItem(item);

                        statsSlot.Item.Delete = true;
                        lookSlot.Item.Delete = true;

                        long newTicketStack = ticketSlot.Stack - 1;
                        if (newTicketStack <= 0)
                        {
                            ticketSlot.Item.Delete = true;

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

                        var combineBagWindow = this.Player.Windows.FirstOrDefault(w => w.Type == Window.WindowTypes.CombineBag);
                        if (combineBagWindow != null) combineBagWindow.Refresh(this.Player, world);

                        break;
                    case "preview":
                        error = ParseRGBA(tokens, out r, out g, out b, out a);
                        if (error != null)
                        {
                            world.Send(this.Player, error);
                            return;
                        }

                        if (!ValidateCustomSlots(world, combineBag, this.Player))
                            return;

                        lookSlot = combineBag.GetSlot(3);

                        int prevx = this.Player.MapX;
                        int prevy = this.Player.MapY;

                        if (prevx == 1) prevx += 1;
                        else prevx -= 1;

                        int pose = this.Player.BodyState;
                        ItemSlot weapon = this.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
                        if (weapon != null)
                        {
                            pose = weapon.Item.BodyState;
                        }

                        if (lookSlot.Item.Slot == ItemTemplate.ItemSlots.OneHanded || lookSlot.Item.Slot == ItemTemplate.ItemSlots.TwoHanded)
                        {
                            pose = lookSlot.Item.BodyState;
                        }

                        world.Send(this.Player,
                            "MKC" + 9000 + "," +
                            "1," +
                            "Custom Preview," +
                            "," +
                            "," +
                            "" + "," + // Guild name
                            prevx + "," +
                            prevy + "," +
                            this.Player.Facing + "," +
                            100 + "," + // HP %
                            this.Player.BodyID + "," +
                            this.Player.BodyR + "," + // Body Color R
                            this.Player.BodyG + "," + // Body Color G
                            this.Player.BodyB + "," + // Body Color B
                            this.Player.BodyA + "," + // Body Color A
                            pose + "," +
                            this.Player.HairID + "," +
                            this.EquippedDisplay(lookSlot, r, g, b, a) + // Note: EquippedDisplay() adds it's own , on end
                            r + "," +
                            g + "," +
                            b + "," +
                            a + "," +
                            "0" + "," + // Invis thing
                            this.Player.FaceID + "," +
                            this.Player.CalculateMoveSpeed() + "," + // Move Speed
                            "0" + "," + // Player Name Color
                            this.MountDisplay(lookSlot, r, g, b, a)); // Mount
                        break;
                    case "kill":
                        world.Send(this.Player, "ERC9000");
                        break;
                }
            }
        }

        public string EquippedDisplay(ItemSlot customLook, int r, int g, int b, int a)
        {
            string e = "";
            EquipSlots[] slots = new EquipSlots[]{EquipSlots.Chest, EquipSlots.Head,
                EquipSlots.Legs, EquipSlots.Feet, EquipSlots.Shield, EquipSlots.Weapon};
            ItemSlot item;

            EquipSlots lookSlot = this.Player.Inventory.ItemSlotToEquipSlot(customLook.Item.Slot);

            foreach (EquipSlots eq in slots)
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
                    item = this.Player.Inventory.GetEquippedSlot(eq);
                    if (item != null)
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

        public string MountDisplay(ItemSlot customLook, int r, int g, int b, int a)
        {
            EquipSlots lookSlot = this.Player.Inventory.ItemSlotToEquipSlot(customLook.Item.Slot);

            ItemSlot item = this.Player.Inventory.GetEquippedSlot(EquipSlots.Mount);
            string e = "";
            if (lookSlot == EquipSlots.Mount)
            {
                e += customLook.Item.GraphicEquipped + "," +
                             r + "," +
                             g + "," +
                             b + "," +
                             a + ",";
            }
            else if (item != null)
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
            if (statsSlot == null || lookSlot == null)
            {
                world.Send(player, "$7Items missing for customisation");
                world.Send(player, "$7In combine bag: Place custom ticket in first slot. Place the item you want the stats of in second slot. Place the item you want the look of in the third slot.");
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
                world.Send(player, "$7Items to be customised must be equipment and must be visible items.");
                return false;
            }

            if (statsSlot.Item.Slot != lookSlot.Item.Slot)
            {
                world.Send(player, "$7Items to be customised must be of the same equipment type.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Skips first token
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string ParseRGBA(string[] tokens, out int r, out int g, out int b, out int a)
        {
            try
            {
                r = Convert.ToInt32(tokens[1]);
                g = Convert.ToInt32(tokens[2]);
                b = Convert.ToInt32(tokens[3]);
                a = Convert.ToInt32(tokens[4]);
            }
            catch (Exception)
            {
                r = -1;
                g = -1;
                b = -1;
                a = -1;
            }

            if (r < 0 || r > 255) return "$7/custom: invalid r value";
            if (g < 0 || g > 255) return "$7/custom: invalid g value";
            if (b < 0 || b > 255) return "$7/custom: invalid b value";
            if (a < 0 || a > 255) return "$7/custom: invalid a value";

            return null;
        }
    }
}