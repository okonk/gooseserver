using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace Goose
{
    /**
     * Holds the spells a player knows
     *
     */
    public class Spellbook
    {
        Spell[] spells;
        /**
         * Lastcast holds when each spell was last cast
         */
        long[] lastcast;
        Player player;

        public Spellbook(Player player)
        {
            this.spells = new Spell[GameWorld.Settings.SpellbookSize + 1];
            this.lastcast = new long[GameWorld.Settings.SpellbookSize + 1];
            this.player = player;
        }

        /**
         * Load, loads spells for player from database
         *
         */
        public void Load(GameWorld world)
        {
            using (var query = world.SqlConnection.CreateCommand())
            {
                query.CommandText = "SELECT serialized_data FROM spellbook WHERE player_id=" + this.player.PlayerID;
                string serialized_data = Convert.ToString(query.ExecuteScalar());
                var spellIds = JsonConvert.DeserializeObject<int[]>(serialized_data, GameWorld.JsonSerializerSettings);

                for (int i = 1; i < this.spells.Length; i++)
                {
                    var spellId = spellIds[i];
                    if (spellId == 0)
                        continue;

                    this.spells[i] = world.SpellHandler.GetSpell(spellId);
                }
            }
        }

        /**
         * Save, saves spells for player into database
         *
         */
        public void Save(GameWorld world)
        {
            var saveSpellbookCommand = world.SqlConnection.CreateCommand();
            saveSpellbookCommand.CommandText =
                @"INSERT INTO spellbook (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                  ON CONFLICT(PLAYER_ID) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
            saveSpellbookCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = this.player.PlayerID });
            saveSpellbookCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = JsonConvert.SerializeObject(spells.Select(s => (s == null ? 0 : s.ID)).ToArray(), GameWorld.JsonSerializerSettings) });
            world.DatabaseWriter.Add(saveSpellbookCommand);
        }

        public int NextFreeSlot(int lowerBound)
        {
            if (lowerBound <= 0 || lowerBound >= GameWorld.Settings.SpellbookSize) return -1;

            for (int i = lowerBound; i <= this.spells.Length; i++)
            {
                if (this.spells[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetNumberOfFreeSlots()
        {
            int free = 0;
            for (int i = 1; i <= GameWorld.Settings.SpellbookSize; i++)
            {
                if (this.spells[i] == null)
                    free++;
            }

            return free;
        }

        /**
         * SendSlot, sends spellbook slot to player
         *
         */
        public void SendSlot(int slot, GameWorld world)
        {
            if (slot < 1 || slot > GameWorld.Settings.SpellbookSize)
            {
                // log bad spell slot
                return;
            }

            Spell spell = this.spells[slot];
            if (spell != null)
            {
                int targetType = 0;
                if (spell.Target == Spell.SpellTargets.Target)
                {
                    if (((int)spell.SpellEffect.Effected & (int)SpellEffect.SpellEffected.Player) != 0)
                    {
                        if (((int)spell.SpellEffect.Effected & (int)SpellEffect.SpellEffected.NPC) != 0)
                            targetType = 2; // NPC and Player
                        else
                            targetType = 3; // Player only
                    }
                    else
                    {
                        targetType = 1; // NPC only
                    }
                }

                world.Send(this.player, P.SpellSlot(spell, slot, targetType));
            }
            else
            {
                world.Send(this.player, P.SpellSlot(null, slot, 0));
            }
        }

        /**
         * SendAll, sends all spell slots to player
         *
         */
        public void SendAll(GameWorld world)
        {
            for (int i = 1; i <= GameWorld.Settings.SpellbookSize; i++)
            {
                this.SendSlot(i, world);
            }
        }

        /**
         * GetSlot, returns spell at slot
         *
         */
        public Spell GetSlot(int slot)
        {
            return this.spells[slot];
        }

        /**
         * GetSlotLastCast, returns spell last cast at slot
         *
         */
        public long GetSlotLastCast(int slot)
        {
            return this.lastcast[slot];
        }

        /**
         * SetSlotLastCast, sets spell last cast at slot
         *
         */
        public void SetSlotLastCast(int slot, long last)
        {
            this.lastcast[slot] = last;
        }

        /**
         * LearnSpell, learns spell if possible
         *
         */
        public bool LearnSpell(int spellid, GameWorld world)
        {
            Spell spell = world.SpellHandler.GetSpell(spellid);
            if (spell == null)
            {
                // log bad spell
                return false;
            }

            return this.AddSpell(spell, world);
        }

        /**
         * AddSpell, Adds spell if possible
         *
         */
        public bool AddSpell(Spell spell, GameWorld world)
        {
            // first pass to check if player knows spell
            foreach (Spell s in this.spells)
            {
                if (s == spell)
                {
                    return false;
                }
            }
            // second pass to check if empty slot to add
            for (int i = 1; i <= GameWorld.Settings.SpellbookSize; i++)
            {
                if (this.spells[i] == null)
                {
                    this.spells[i] = spell;
                    this.lastcast[i] = 0;

                    this.SendSlot(i, world);

                    world.Send(this.player, P.ServerMessage("You have learned " + spell.Name + "."));
                    return true;
                }
            }

            return false;
        }

        /**
         * RemoveSpell, removes spell at slot
         *
         */
        public bool RemoveSpell(int slot, GameWorld world)
        {
            if (slot <= 0 || slot > GameWorld.Settings.SpellbookSize) return false;

            if (this.spells[slot] != null)
            {
                world.Send(this.player, P.ServerMessage("You have forgotten " + this.spells[slot].Name + "."));

                this.spells[slot] = null;
                this.lastcast[slot] = 0;

                this.SendSlot(slot, world);

                return true;
            }

            return false;
        }

        /**
         * SwapSlots, swaps two slots in spellbook
         *
         */
        public void SwapSlots(int slot1, int slot2, GameWorld world)
        {
            if (slot1 <= 0 || slot1 > GameWorld.Settings.SpellbookSize ||
                slot2 <= 0 || slot2 > GameWorld.Settings.SpellbookSize)
            {
                return;
            }

            Spell spell1 = this.spells[slot1];
            long aether1 = this.lastcast[slot1];
            Spell spell2 = this.spells[slot2];
            long aether2 = this.lastcast[slot2];

            this.spells[slot1] = spell2;
            this.spells[slot2] = spell1;
            this.lastcast[slot1] = aether2;
            this.lastcast[slot2] = aether1;

            this.SendSlot(slot1, world);
            this.SendSlot(slot2, world);
        }


        public void RemoveNonClassSpells(GameWorld world)
        {
            Spell slot;

            for (int i = 1; i <= GameWorld.Settings.SpellbookSize; i++)
            {
                slot = this.GetSlot(i);

                if (slot == null) continue;

                if ((slot.ClassRestrictions & Convert.ToInt64(Math.Pow(2.0, (double)this.player.Class.ClassID))) != 0)
                {
                    this.spells[i] = null;
                    this.lastcast[i] = 0;

                    this.SendSlot(i, world);
                }
            }
        }
    }
}
