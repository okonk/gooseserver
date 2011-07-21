using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

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
            this.spells = new Spell[GameSettings.Default.SpellbookSize + 1];
            this.lastcast = new long[GameSettings.Default.SpellbookSize + 1];
            this.player = player;
        }

        /**
         * Load, loads spells for player from database
         * 
         */
        public void Load(GameWorld world)
        {
            SqlCommand command = new SqlCommand(
                "SELECT * FROM spellbook WHERE player_id=" + this.player.PlayerID, 
                world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            int slot, spellid;

            while (reader.Read())
            {
                spellid = Convert.ToInt32(reader["spell_id"]);
                slot = Convert.ToInt32(reader["slot"]);

                if (slot < 1 || slot > GameSettings.Default.SpellbookSize)
                {
                    // log bad spellbook loading
                    continue;
                }

                this.spells[slot] = world.SpellHandler.GetSpell(spellid);
                if (this.spells[slot] == null)
                {
                    // log bad spell loading
                    continue;
                }

                this.lastcast[slot] = Convert.ToInt64(reader["last_casted"]);
            }

            reader.Close();
        }

        /**
         * Save, saves spells for player into database
         * 
         */
        public void Save(GameWorld world)
        {
            SqlCommand command = new SqlCommand("", world.SqlConnection);
            string query;
            Spell slot;

            // Save spells
            for (int i = 1; i <= GameSettings.Default.SpellbookSize; i++)
            {
                slot = this.GetSlot(i);
                if (slot == null)
                {
                    query = "DELETE FROM spellbook WHERE player_id=" + this.player.PlayerID + " AND slot=" + i;
                }
                else
                {
                    query = "DELETE FROM spellbook WHERE player_id=" +
                        this.player.PlayerID + " AND slot=" + i + ";";

                    query += "INSERT INTO spellbook (player_id, slot, spell_id, last_casted) VALUES (" +
                        this.player.PlayerID + ", " +
                        i + ", " +
                        slot.ID + ", " +
                        this.GetSlotLastCast(i) + ");";
                }

                command = new SqlCommand(query, world.SqlConnection);
                command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
            }
        }

        /**
         * SendSlot, sends spellbook slot to player
         * 
         */
        public void SendSlot(int slot, GameWorld world)
        {
            if (slot < 1 || slot > GameSettings.Default.SpellbookSize)
            {
                // log bad spell slot
                return;
            }

            Spell spell = this.spells[slot];
            if (spell == null)
            {
                world.Send(this.player, "SSS" + slot);
            }
            else
            {
                world.Send(this.player, "SSS" + slot + "," + 
                    spell.Name + ",0,0," + 
                    (spell.Target == Spell.SpellTargets.Target ? "T" : "X") + "," +
                    spell.Graphic);
            }
        }

        /**
         * SendAll, sends all spell slots to player
         * 
         */
        public void SendAll(GameWorld world)
        {
            for (int i = 1; i <= GameSettings.Default.SpellbookSize; i++)
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
            for (int i = 1; i <= GameSettings.Default.SpellbookSize; i++)
            {
                if (this.spells[i] == null)
                {
                    this.spells[i] = spell;
                    this.lastcast[i] = 0;

                    this.SendSlot(i, world);

                    world.Send(this.player, "$7You have learned " + spell.Name + ".");
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
            if (slot <= 0 || slot > GameSettings.Default.SpellbookSize) return false;

            if (this.spells[slot] != null)
            {
                world.Send(this.player, "$7You have unlearned " + this.spells[slot].Name + ".");

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
            if (slot1 <= 0 || slot1 > GameSettings.Default.SpellbookSize ||
                slot2 <= 0 || slot2 > GameSettings.Default.SpellbookSize) 
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

            for (int i = 1; i <= GameSettings.Default.SpellbookSize; i++)
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
