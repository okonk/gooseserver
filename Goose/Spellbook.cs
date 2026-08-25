using System.Text;
using System.Data;
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
        private GooseSettings settings;

        public Spellbook(Player player, GooseSettings settings)
        {
            this.spells = new Spell[settings.SpellbookSize + 1];
            this.lastcast = new long[settings.SpellbookSize + 1];
            for (int i = 0; i < this.lastcast.Length; i++)
                this.lastcast[i] = long.MinValue >> 1;
            this.player = player;
            this.settings = settings;
        }

        /**
         * Load, loads spells for player from database
         *
         */
        public void Load(GameWorld world)
        {
            int playerId = this.player.PlayerID;
            world.Database.Execute(conn =>
            {
                using var query = conn.CreateCommand();
                query.CommandText = "SELECT serialized_data FROM spellbook WHERE player_id=" + playerId;
                string serialized_data = Convert.ToString(query.ExecuteScalar());
                var spellIds = JsonHelper.Deserialize<int[]>(serialized_data);

                for (int i = 1; i < this.spells.Length; i++)
                {
                    var spellId = spellIds[i];
                    if (spellId == 0)
                        continue;

                    this.spells[i] = world.SpellHandler.GetSpell(spellId);
                }
            });
        }

        /**
         * Save, saves spells for player into database
         *
         */
        public void Save(GameWorld world)
        {
            world.Database.Enqueue(this.BuildSave());
        }

        /**
         * BuildSave, snapshots state and returns the work to persist it
         *
         * Returned rather than enqueued so a full player save can run every part inside
         * one transaction. Snapshotting happens here, on the game thread, so later
         * mutations do not race the DB thread.
         *
         */
        public Action<SQLiteConnection> BuildSave()
        {
            int playerId = this.player.PlayerID;
            string serialized = JsonHelper.Serialize(spells.Select(s => (s is null ? 0 : s.ID)).ToArray());

            return conn =>
            {
                using var saveSpellbookCommand = conn.CreateCommand();
                saveSpellbookCommand.CommandText =
                    @"INSERT INTO spellbook (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                      ON CONFLICT(PLAYER_ID) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
                saveSpellbookCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = playerId });
                saveSpellbookCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = serialized });
                saveSpellbookCommand.ExecuteNonQuery();
            };
        }

        public int NextFreeSlot(int lowerBound)
        {
            if (lowerBound <= 0 || lowerBound >= this.settings.SpellbookSize) return -1;

            for (int i = lowerBound; i < this.spells.Length; i++)
            {
                if (this.spells[i] is null)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetNumberOfFreeSlots()
        {
            int free = 0;
            for (int i = 1; i <= this.settings.SpellbookSize; i++)
            {
                if (this.spells[i] is null)
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
            if (slot < 1 || slot > this.settings.SpellbookSize)
            {
                // log bad spell slot
                return;
            }

            Spell spell = this.spells[slot];
            if (spell is not null)
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
            for (int i = 1; i <= this.settings.SpellbookSize; i++)
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
            if (spell is null)
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
            foreach (var s in this.spells)
            {
                if (s == spell)
                {
                    return false;
                }
            }
            // second pass to check if empty slot to add
            for (int i = 1; i <= this.settings.SpellbookSize; i++)
            {
                if (this.spells[i] is null)
                {
                    this.spells[i] = spell;
                    this.lastcast[i] = long.MinValue >> 1;

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
            if (slot <= 0 || slot > this.settings.SpellbookSize) return false;

            if (this.spells[slot] is not null)
            {
                world.Send(this.player, P.ServerMessage("You have forgotten " + this.spells[slot].Name + "."));

                this.spells[slot] = null;
                this.lastcast[slot] = long.MinValue >> 1;

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
            if (slot1 <= 0 || slot1 > this.settings.SpellbookSize ||
                slot2 <= 0 || slot2 > this.settings.SpellbookSize)
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

            for (int i = 1; i <= this.settings.SpellbookSize; i++)
            {
                slot = this.GetSlot(i);

                if (slot is null) continue;

                if (!this.player.Class.CanUse(slot.ClassRestrictions))
                {
                    this.spells[i] = null;
                    this.lastcast[i] = long.MinValue >> 1;

                    this.SendSlot(i, world);
                }
            }
        }
    }
}
