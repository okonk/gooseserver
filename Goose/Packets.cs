using System.Text;

namespace Goose
{
    public static class P
    {
        public static Func<string, string> LoginAccepted = (message) => { return "LOK" + message; };
        public static Func<string, string> LoginDenied = (message) => { return "LNO" + message; };
        public static Func<string, string> ServerMessage = (message) => { return "$7" + message; };
        public static Func<string, string> GroupMessage = (message) => { return "$3" + message; };
        public static Func<string, string> GuildMessage = (message) => { return "$2" + message; };
        public static Func<string, string> TellMessage = (message) => { return "$6" + message; };

        // The 0 in here is for AFK, but I don't see the use of it
        public static Func<Player, string, string> Tell = (player, message) => { return $"&{player.Name},0,{message}"; };

        // TODO: What is the hash? -- White/normal text
        public static Func<string, string> HashMessage = (message) => { return "#" + message; };
        public static Func<int, int, int, string> SpellPlayer = (loginId, animationId, animationFile) => { return "SPP" + loginId + "," + animationId + "," + animationFile; };
        public static Func<int, int, int, int, string> SpellTile = (x, y, animationId, animationFile) =>
        {
            return "SPA" +
                x + "," +
                y + "," +
                animationId + "," +
                animationFile;
        };

        public static Func<Player, string> ExpBar = (player) =>
        {
            long percent, tnl, exp;
            if (player.Class.GetLevel(player.Level).Experience == 0)
            {
                percent = 100;
                tnl = exp = player.Experience;
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

            return "TNL" + Math.Min(100, percent) + "," + exp + "," + tnl + "," + player.ExperienceSold;
        };

        public static Func<int, string> EraseCharacter = (loginId) => { return "ERC" + loginId; };
        public static Func<int, string, string, string> Chat = (loginId, name, message) => { return "^" + loginId + "," + name + ": " + message; };
        public static Func<string> DoneSendingMap = () => { return "DSM"; };
        public static Func<int, string> SetYourCharacter = (loginId) => { return "SUC" + loginId; };
        public static Func<Player, string> SetYourPosition = (player) => { return "SUP" + player.MapX + "," + player.MapY; };
        // TODO: No clue what "AMA" stands for. AdminModeActivate?
        public static Func<int, string> AdminMode = (loginId) => { return "AMA" + loginId + ",1"; };

        /**
         * MKCString, returns the MKC packet string for this character
         *
         * MKCid,character type,name,title,surname,guild,x,y,facing,hp percent,body,
         * body pose,hair id,chest id,chest r,g,b,a,helm id,helm r,g,b,a,
         * pants id,pants r,g,b,a,shoes id,shoes r,g,b,a,
         * shield id,shield r,g,b,a,weapon id,weapon r,g,b,a,hair_r,hair_g,hair_b,hair_a,invis,head
         *
         * character type = 1 for player, 2 for regular npc, some others for banker and vendors will find later
         * hair id = 20ish for the normal hairs
         * head = 70-73 for faces
         * body pose/state = 1 for normal, 3 for staff, 4 for sword
         * body = values 100-166 are illusions, 1 is male, 11 is female. 2/12 are naga. 3 is skeleton
         * invis = 1 while the character currently has an Invisible buff (IsInvisible)
         *
         * For item r,g,b,a of 0,0,0,0 you can use * instead
         *
         */
        public static Func<Player, string> MakeCharacter = (player) =>
        {
            // Hack to get around the fact pets are a subclass of Player
            if (player is Pet) return MakePetCharacter((Pet)player);

            int pose = player.BodyState;
            ItemSlot weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon is not null)
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
                          (int)(((float)player.CurrentHP / player.MaxHP) * 100) + "," + // HP %
                          player.CurrentBodyID + "," +
                          player.BodyR + "," + // Body Color R
                          player.BodyG + "," + // Body Color G
                          player.BodyB + "," + // Body Color B
                          player.BodyA + "," + // Body Color A
                          (player.CurrentBodyID >= 100 ? 3 : pose) + "," +
                          (player.CurrentBodyID >= 100 ? "" : player.HairID + ",") +
                          (player.CurrentBodyID >= 100 ? "" : player.Inventory.EquippedDisplay()) + // Note: EquippedDisplay() adds it's own , on end
                          (player.CurrentBodyID >= 100 ? "" : player.HairR + "," + player.HairG + "," + player.HairB + "," + player.HairA + ",") +
                          (player.IsInvisible ? "1" : "0") + "," + // Invisible
                          (player.CurrentBodyID >= 100 ? "" : player.FaceID + ",") +
                          player.CalculateMoveSpeed() + "," + // Move Speed
                          (player.Access > Player.AccessStatus.Normal ? "1" : "0") + "," + // Is GM
                          (player.CurrentBodyID >= 100 ? "" : player.Inventory.MountDisplay()); // Mount
        };

        public static Func<Player, string> UpdateCharacter = (player) =>
        {
            if (player is Pet) return UpdatePet((Pet)player);

            int pose = player.BodyState;
            ItemSlot weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon is not null)
            {
                pose = weapon.Item.BodyState;
            }

            return "CHP" +
                   player.LoginID + "," +
                   player.CurrentBodyID + "," +
                   player.BodyR + "," + // Body Color R
                   player.BodyG + "," + // Body Color G
                   player.BodyB + "," + // Body Color B
                   player.BodyA + "," + // Body Color A
                   (player.CurrentBodyID >= 100 ? 3 : pose) + "," +
                   (player.CurrentBodyID >= 100 ? "" : player.HairID + ",") +
                   (player.CurrentBodyID >= 100 ? "" : player.Inventory.EquippedDisplay()) + // Note: EquippedDisplay() adds it's own , on end
                   (player.CurrentBodyID >= 100 ? "" : player.HairR + ",") +
                   (player.CurrentBodyID >= 100 ? "" : player.HairG + ",") +
                   (player.CurrentBodyID >= 100 ? "" : player.HairB + ",") +
                   (player.CurrentBodyID >= 100 ? "" : player.HairA + ",") +
                   (player.IsInvisible ? "1" : "0") + "," + // Invisible
                   (player.CurrentBodyID >= 100 ? "" : player.FaceID + ",") +
                   player.CalculateMoveSpeed() + "," + // Move Speed
                   (player.CurrentBodyID >= 100 ? "" : player.Inventory.MountDisplay()); // Mount
        };

        public static Func<NPC, string> UpdateNPC = (npc) =>
        {
            return "CHP" +
                   npc.LoginID + "," +
                   npc.CurrentBodyID + "," +
                   npc.BodyR + "," + // Body Color R
                   npc.BodyG + "," + // Body Color G
                   npc.BodyB + "," + // Body Color B
                   npc.BodyA + "," + // Body Color A
                   (npc.CurrentBodyID >= 100 ? 3 : npc.BodyState) + "," +
                   (npc.CurrentBodyID >= 100 ? "" : npc.HairID + ",") +
                   (npc.CurrentBodyID >= 100 ? "" : npc.EquippedItems + ",") +
                   (npc.CurrentBodyID >= 100 ? "" : npc.HairR + ",") +
                   (npc.CurrentBodyID >= 100 ? "" : npc.HairG + ",") +
                   (npc.CurrentBodyID >= 100 ? "" : npc.HairB + ",") +
                   (npc.CurrentBodyID >= 100 ? "" : npc.HairA + ",") +
                   (npc.IsInvisible ? "1" : "0") + "," + // Invisible
                   (npc.CurrentBodyID >= 100 ? "" : npc.FaceID + ",") +
                   "320," + // Move Speed
                   (npc.CurrentBodyID >= 100 ? "" : "0,0,0,0,0"); // Mount
        };

        public static Func<NPC, string> MakeNPCCharacter = (npc) =>
        {
            return "MKC" + npc.LoginID + "," +
                        (int)npc.NPCType + "," +
                        npc.Name + "," +
                        npc.Title + "," +
                        npc.Surname + "," +
                        "" + "," + // Guild name
                        npc.MapX + "," +
                        npc.MapY + "," +
                        npc.Facing + "," +
                        (int)(((float)npc.CurrentHP / npc.MaxHP) * 100) + "," + // HP %
                        npc.CurrentBodyID + "," +
                        npc.BodyR + "," + // Body Color R
                        npc.BodyG + "," + // Body Color G
                        npc.BodyB + "," + // Body Color B
                        npc.BodyA + "," + // Body Color A
                        (npc.CurrentBodyID >= 100 ? 3 : npc.BodyState) + "," +
                        (npc.CurrentBodyID >= 100 ? "" : npc.HairID + ",") +
                        (npc.CurrentBodyID >= 100 ? "" : npc.EquippedItems + ",") +
                        (npc.CurrentBodyID >= 100 ? "" : npc.HairR + "," + npc.HairG + "," + npc.HairB + "," + npc.HairA + ",") +
                        (npc.IsInvisible ? "1" : "0") + "," + // Invisible
                        (npc.CurrentBodyID >= 100 ? "" : npc.FaceID + ",") +
                        "320," + // Move Speed
                        "0" + "," + // Player Name Color
                        (npc.CurrentBodyID >= 100 ? "" : "0,0,0,0,0"); // Mount
        };

        public static Func<Pet, string> MakePetCharacter = (npc) =>
        {
            return "MKC" + npc.LoginID + "," +
                        "13" + "," +
                        npc.Name + "," +
                        npc.Title + "," +
                        npc.Surname + "," +
                        "" + "," + // Guild name
                        npc.MapX + "," +
                        npc.MapY + "," +
                        npc.Facing + "," +
                        (int)(((float)npc.CurrentHP / npc.MaxHP) * 100) + "," + // HP %
                        npc.CurrentBodyID + "," +
                        npc.BodyR + "," + // Body Color R
                        npc.BodyG + "," + // Body Color G
                        npc.BodyB + "," + // Body Color B
                        npc.BodyA + "," + // Body Color A
                        (npc.CurrentBodyID >= 100 ? 3 : npc.BodyState) + "," +
                        (npc.CurrentBodyID >= 100 ? "" : npc.HairID + ",") +
                        (npc.CurrentBodyID >= 100 ? "" : npc.EquippedItems + ",") +
                        (npc.CurrentBodyID >= 100 ? "" : npc.HairR + "," + npc.HairG + "," + npc.HairB + "," + npc.HairA + ",") +
                        (npc.IsInvisible ? "1" : "0") + "," + // Invisible
                        (npc.CurrentBodyID >= 100 ? "" : npc.FaceID + ",") +
                        "320," + // Move Speed
                        "0" + "," + // Player Name Color
                        (npc.CurrentBodyID >= 100 ? "" : "0,0,0,0,0"); // Mount
        };

        public static Func<Pet, string> UpdatePet = (pet) =>
        {
            return "CHP" +
                   pet.LoginID + "," +
                   pet.CurrentBodyID + "," +
                   pet.BodyR + "," + // Body Color R
                   pet.BodyG + "," + // Body Color G
                   pet.BodyB + "," + // Body Color B
                   pet.BodyA + "," + // Body Color A
                   (pet.CurrentBodyID >= 100 ? 3 : pet.BodyState) + "," +
                   (pet.CurrentBodyID >= 100 ? "" : pet.HairID + ",") +
                   (pet.CurrentBodyID >= 100 ? "" : pet.EquippedItems + ",") +
                   (pet.CurrentBodyID >= 100 ? "" : pet.HairR + ",") +
                   (pet.CurrentBodyID >= 100 ? "" : pet.HairG + ",") +
                   (pet.CurrentBodyID >= 100 ? "" : pet.HairB + ",") +
                   (pet.CurrentBodyID >= 100 ? "" : pet.HairA + ",") +
                   (pet.IsInvisible ? "1" : "0") + "," + // Invisible
                   (pet.CurrentBodyID >= 100 ? "" : pet.FaceID + ",") +
                   "320," + // Move Speed
                   (pet.CurrentBodyID >= 100 ? "" : "0,0,0,0,0"); // Mount
        };

        public static Func<bool, string> SeeInvisible = (canSee) => "SINVS" + (canSee ? "1" : "0");

        public static Func<Player, string> WeaponSpeed = (player) =>
        {
            int wps = (int)((player.WeaponDelay / 10.0m * (1 - Math.Min(0.95m, player.MaxStats.Haste))) * 1000);

            return "WPS" + wps + ",0,0";
        };

        public static Func<ItemTile, string> MakeObject = (tile) =>
        {
            string rgba = "*";
            if (tile.ItemSlot.Item.GraphicA != 0)
            {
                rgba = tile.ItemSlot.Item.GraphicR + "," +
                tile.ItemSlot.Item.GraphicG + "," +
                tile.ItemSlot.Item.GraphicB + "," +
                tile.ItemSlot.Item.GraphicA;
            }

            return "DOB" +
                   tile.ItemSlot.Item.GraphicTile + "," +
                   tile.ItemSlot.Item.GraphicFile + "," +
                   0 + "," + // sound id
                   0 + "," + // sound file
                   tile.X + "," +
                   tile.Y + "," +
                   "," + // title
                   tile.ItemSlot.Item.Name + "," +
                   "," + // surname
                   tile.ItemSlot.Stack + "," +
                   (int)tile.ItemSlot.Item.Type + "," +
                   tile.ItemSlot.Item.Flags + "," +
                   (false ? 1 : 0) + "," + // drop animation if 1
                   rgba;
        };

        public static Func<ItemTile, string> EraseObject = (tile) =>
        {
            return "EOB" + tile.X + "," + tile.Y;
        };

        public static Func<Player, string, string> Emote = (player, data) =>
        {
            string[] tokens = data.Split(',');
            if (tokens.Length != 2) return null;

            int animId = 0;
            if (!int.TryParse(tokens[0], out animId) || animId < 1080 || animId > 1091) return null;

            int sheetNum = 0;
            if (!int.TryParse(tokens[1], out sheetNum) || sheetNum < 8 || sheetNum > 10) return null;

            return $"EMOT{player.LoginID},{animId},{sheetNum}";
        };

        public static Func<ICharacter, string> BattleTextStunned = (target) =>
        {
            return "BT" + target.LoginID + ",50";
        };

        public static Func<ICharacter, string> BattleTextRooted = (target) =>
        {
            return "BT" + target.LoginID + ",11";
        };

        public static Func<ICharacter, string> BattleTextDodge = (target) =>
        {
            return "BT" + target.LoginID + ",20";
        };

        public static Func<ICharacter, string> BattleTextMiss = (target) =>
        {
            return "BT" + target.LoginID + ",21";
        };

        public static Func<ICharacter, long, string> BattleTextDamage = (target, damage) =>
        {
            return "BT" + target.LoginID + ",1," + (-damage);
        };

        public static Func<ICharacter, long, string> BattleTextHeal = (target, heal) =>
        {
            return "BT" + target.LoginID + ",7,+" + (-heal);
        };

        public static Func<ICharacter, string, string> BattleTextYellow = (target, message) =>
        {
            return "BT" + target.LoginID + ",60," + message;
        };

        public static Func<ICharacter, string> ChangeHeading = (target) =>
        {
            return "CHH" + target.LoginID + "," + target.Facing;
        };

        public static Func<ICharacter, string> MoveCharacter = (target) =>
        {
            return "MOC" + target.LoginID + "," + target.MapX + "," + target.MapY;
        };

        public static Func<ICharacter, string> Attack = (target) =>
        {
            return "ATT" + target.LoginID;
        };

        public static Func<ICharacter, string> Cast = (target) =>
        {
            return "CST" + target.LoginID;
        };

        public static Func<ICharacter, string> NPCAngryEmote = (target) =>
        {
            return "EMOT" + target.LoginID + ",1087,9";
        };

        /**
         * SNFString, send info string
         *
         * SNFguildname,,classname,level,max_hp,max_mp,max_sp,cur_,cur_mp,cur_sp,
         * stat_str,stat_sta,stat_int,stat_dex,ac,res_f,res_w,res_e,res_a,res_s,gold
         */
        public static Func<Player, string> StatusInfo = (player) =>
        {
            return "SNF" +
                   (player.Guild is not null ? player.Guild.Name : "") + "," + // guild name
                   "" + "," + // Not sure
                   player.Class.ClassName + "," +
                   player.Level + "," +
                   player.MaxHP + "," +
                   player.MaxMP + "," +
                   player.MaxSP + "," +
                   player.CurrentHP + "," +
                   player.CurrentMP + "," +
                   player.CurrentSP + "," +
                   player.MaxStats.Strength + "," +
                   player.MaxStats.Stamina + "," +
                   player.MaxStats.Intelligence + "," +
                   player.MaxStats.Dexterity + "," +
                   player.MaxStats.AC + "," +
                   player.MaxStats.FireResist + "," +
                   player.MaxStats.WaterResist + "," +
                   player.MaxStats.EarthResist + "," +
                   player.MaxStats.AirResist + "," +
                   player.MaxStats.SpiritResist + "," +
                   player.Gold;
        };

        public static Func<Map, string> SendCurrentMap = (map) =>
        {
            return "SCM" + map.FileName + ",1," + map.Name + ",0";
        };

        // Wire format: MFL<pvp>,<items>,<cast> — 1 = enabled
        public static Func<Map, string> SendMapFlags = (map) =>
        {
            return "MFL" + (map.CanPVP ? "1" : "0") + "," +
                        (map.CanUseItems ? "1" : "0") + "," +
                        (map.CanCast ? "1" : "0");
        };

        public static Func<Class, string> ClassUpdate = (@class) =>
        {
            return "CUP" + @class.ClassID + "," + @class.ClassName;
        };

        public static Func<ICharacter, string> VitalsPercentage = (target) =>
        {
            return "VPU" + target.LoginID + "," +
                   (int)(((float)target.CurrentHP / target.MaxHP) * 100) + "," +
                   (int)(((float)target.CurrentMP / target.MaxMP) * 100);
        };

        public static Func<int, int, string, string> WindowTextLine = (windowId, lineNo, line) =>
        {
            return $"WNF{windowId},{lineNo},{line}|0|0|0|0|*";
        };

        public static Func<Item, GameWorld, int, long, string> ItemSlot = (item, world, slotId, stack) =>
        {
            var spellEffect = item.SpellEffect;
            int spellEffectChance = (int)item.SpellEffectChance;
            if (item.LearnSpellID != 0)
            {
                var spell = world.SpellHandler.GetSpell(item.LearnSpellID);
                spellEffect = spell?.SpellEffect;
                spellEffectChance = 100;
            }

            return slotId + "|" +
                    item.GraphicTile + "|" +
                    item.GraphicFile + "|" +
                    "" + "|" + // title
                    item.Name + "|" +
                    "" + "|" + //surname
                    stack + "|" +
                    item.Value + "|" +
                    item.Flags + "|" +
                    item.Description + "|" +
                    item.TotalWeaponDamage + "|" +
                    item.TotalWeaponDamage + "|" +
                    (item.TotalWeaponDamage > 0 ? item.WeaponDelay : 0) + "|" +
                    (int)item.Type + "|" +
                    item.TotalStats.AC + "|" +
                    item.TotalStats.HP + "|" +
                    item.TotalStats.MP + "|" +
                    item.TotalStats.SP + "|" +
                    item.TotalStats.Strength + "|" +
                    item.TotalStats.Stamina + "|" +
                    item.TotalStats.Intelligence + "|" +
                    item.TotalStats.Dexterity + "|" +
                    item.TotalStats.FireResist + "|" +
                    item.TotalStats.WaterResist + "|" +
                    item.TotalStats.EarthResist + "|" +
                    item.TotalStats.AirResist + "|" +
                    item.TotalStats.SpiritResist + "|" +
                    item.MinLevel + "|" +
                    item.MaxLevel + "|" +
                    ItemTemplate.FigureClassRestrictions(world, item.ClassRestrictions) +
                    "0" + "|" + // gm access
                    "0" + "|" + // gender, always 0 since we don't care about gender
                    (spellEffect is null ? "" : spellEffect.Name + ';' + string.Join(";", spellEffect.GetItemDescription(world))) + "|" +
                    spellEffectChance + "|" +
                    item.BodyType + "|" +
                    (int)item.UseType + "|" +
                    0 + "|" + // not sure
                    item.GraphicR + "|" +
                    item.GraphicG + "|" +
                    item.GraphicB + "|" +
                    item.GraphicA + "|" +
                    // The name the client labels Value with. No vendor is in scope here, so
                    // this is the item's own currency - which is the right answer anyway,
                    // since an item override wins wherever it is traded (CurrencyHandler.cs:41).
                    world.CurrencyHandler.Resolve(item.Template, null).Name;
        };

        /// <summary>The vendor is threaded in only to name the currency: a credit dealer's
        /// stock carries no item-level currency, so the label has to come from the NPC.</summary>
        public static Func<ItemTemplate, GameWorld, NPC, int, long, string> VendorItemSlot = (item, world, vendor, slotId, stack) =>
        {
            var spellEffect = item.SpellEffect;
            int spellEffectChance = (int)item.SpellEffectChance;
            if (item.LearnSpellID != 0)
            {
                var spell = world.SpellHandler.GetSpell(item.LearnSpellID);
                spellEffect = spell?.SpellEffect;
                spellEffectChance = 100;
            }

            return slotId + "|" +
                    item.GraphicTile + "|" +
                    item.GraphicFile + "|" +
                    "" + "|" + // title
                    item.Name + "|" +
                    "" + "|" + //surname
                    stack + "|" +
                    item.Value + "|" +
                    item.Flags + "|" +
                    item.Description + "|" +
                    item.WeaponDamage + "|" +
                    item.WeaponDamage + "|" +
                    (item.WeaponDamage > 0 ? item.WeaponDelay : 0) + "|" +
                    (int)item.Type + "|" +
                    item.BaseStats.AC + "|" +
                    item.BaseStats.HP + "|" +
                    item.BaseStats.MP + "|" +
                    item.BaseStats.SP + "|" +
                    item.BaseStats.Strength + "|" +
                    item.BaseStats.Stamina + "|" +
                    item.BaseStats.Intelligence + "|" +
                    item.BaseStats.Dexterity + "|" +
                    item.BaseStats.FireResist + "|" +
                    item.BaseStats.WaterResist + "|" +
                    item.BaseStats.EarthResist + "|" +
                    item.BaseStats.AirResist + "|" +
                    item.BaseStats.SpiritResist + "|" +
                    item.MinLevel + "|" +
                    item.MaxLevel + "|" +
                    ItemTemplate.FigureClassRestrictions(world, item.ClassRestrictions) +
                    "0" + "|" + // gm access
                    "0" + "|" + // gender, always 0 since we don't care about gender
                    (spellEffect is null ? "" : spellEffect.Name + ';' + string.Join(";", spellEffect.GetItemDescription(world))) + "|" +
                    spellEffectChance + "|" +
                    item.BodyType + "|" +
                    (int)item.UseType + "|" +
                    0 + "|" + // not sure
                    item.GraphicR + "|" +
                    item.GraphicG + "|" +
                    item.GraphicB + "|" +
                    item.GraphicA + "|" +
                    world.CurrencyHandler.Resolve(item, vendor).Name;
        };

        public static Func<Window, Item, GameWorld, int, long, string> BankSlot = (window, item, world, slotId, stack) =>
        {
            return "SBS" + ItemSlot(item, world, slotId, stack);
        };

        public static Func<Window, int, string> ClearBankSlot = (window, slotId) =>
        {
            return "CBS" + slotId;
        };

        public static Func<Item, GameWorld, int, long, string> InventorySlot = (item, world, slotId, stack) =>
        {
            return "SIS" + ItemSlot(item, world, slotId, stack);
        };

        public static Func<int, string> ClearInventorySlot = (slotId) =>
        {
            return "CIS" + slotId;
        };

        public static Func<Item, GameWorld, int, long, string> EquipSlot = (item, world, slotId, stack) =>
        {
            return "SIS" + ItemSlot(item, world, slotId + 31, stack);
        };

        public static Func<int, string> ClearEquipSlot = (slotId) =>
        {
            return "CIS" + (slotId + 31);
        };

        public static Func<Window, Item, GameWorld, int, long, string> CombineSlot = (window, item, world, slotId, stack) =>
        {
            return "SCS" + ItemSlot(item, world, slotId, stack);
        };

        public static Func<Window, int, string> ClearCombineSlot = (window, slotId) =>
        {
            return "CCS" + slotId;
        };


        public static Func<Window, ItemTemplate, GameWorld, int, long, string> VendorSlot = (window, item, world, slotId, stack) =>
        {
            return "SVS" + VendorItemSlot(item, world, window.NPC, slotId, stack);
        };

        public static Func<string> ClearVendor = () =>
        {
            return "VCL";
        };

        public static Func<Spell, int, int, string> SpellSlot = (spell, slotId, targetType) =>
        {
            if (spell is null)
            {
                return "SSS" + slotId + ",,0,0,0,0,0,0,0";
            }

            return "SSS" + slotId + "," +
                    spell.Name + "," +
                    spell.SpellEffect.Animation + "," +
                    "1000," +
                    "0," +
                    targetType + "," +
                    spell.Graphic + "," +
                    spell.GraphicFile + "," +
                    (spell.Aether > TimeSpan.FromHours(1).TotalMilliseconds ? 5000 : spell.Aether);
        };

        public static Func<Player, int, string> GroupUpdate = (player, index) =>
        {
            if (player is null)
            {
                return "GUD" + index + ",0,,0,";
            }

            return "GUD" + index + "," + player.LoginID + "," + player.Name + "," + player.Level + "," + player.Class.ClassName;
        };

        public static Func<Buff, int, string> BuffBar = (buff, index) =>
        {
            if (buff is null)
            {
                return "BUF" + index;
            }

            return "BUF" + index + "," + buff.SpellEffect.BuffGraphic + "," + buff.SpellEffect.BuffGraphicFile + "," + buff.SpellEffect.Name;
        };

        public static Func<Window, string> MakeWindow = (window) =>
        {
            return "MKW" +
                    window.ID + "," +
                    (int)window.Frame + "," +
                    window.Title + "," +
                    window.Buttons + "," +
                    (window.NPC?.LoginID ?? 0) + "," +
                    "0,0";
        };

        public static Func<Window, string> EndWindow = (window) =>
        {
            return "ENW" + window.ID;
        };
    }
}
