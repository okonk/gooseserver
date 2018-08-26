using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public class PlayerBank
    {
        /// <summary>
        /// Maps an NPC ID to the ItemContainer containing the bank's items
        /// </summary>
        private Dictionary<int, ItemContainer> bankContainers;

        public PlayerBank()
        {
            this.bankContainers = new Dictionary<int, ItemContainer>();
        }

        public void Load(GameWorld world, Player player)
        {
            SqlCommand command = new SqlCommand(
                "SELECT * FROM bank_items WHERE player_id=" + player.PlayerID,
                world.SqlConnection);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int npc_id = Convert.ToInt32(reader["npc_id"]);
                    int item_id = Convert.ToInt32(reader["item_id"]);
                    int slot = Convert.ToInt16(reader["slot"]);
                    int stack = Convert.ToInt32(reader["stack"]);

                    if (slot < 1 || slot > player.NumberOfBankPages * BankWindow.SlotsPerPage + 1)
                    {
                        // log bad bank loading
                        continue;
                    }

                    if (stack < 1) continue; // log bad stack

                    Item item = world.ItemHandler.GetItem(item_id);
                    if (item == null) continue; // log bad item id

                    ItemContainer container = GetOrCreateContainer(player, npc_id);
                    //if (container.GetSlot(slot) != null) continue; // log 2 items trying to be in the same slot

                    var itemSlot = new ItemSlot { Item = item, Stack = stack };
                    container.SetSlot(slot, itemSlot);
                }
            }
        }

        public void Save(GameWorld world, Player player)
        {
            foreach (var kvp in this.bankContainers)
            {
                int npc_id = kvp.Key;
                ItemContainer container = kvp.Value;

                for (int i = 1; i < container.MaxSlots; i++)
                {
                    string query = null;

                    var slot = container.GetSlot(i);
                    if (slot == null)
                    {
                        query = "DELETE FROM bank_items WHERE npc_id=" + npc_id + " AND player_id=" + player.PlayerID + " AND slot=" + i;
                    }
                    else
                    {
                        if (slot.Item.Unsaved) slot.Item.AddItem(world); // have to add item

                        query = "DELETE FROM bank_items WHERE npc_id=" + npc_id + " AND player_id=" +
                            player.PlayerID + " AND slot=" + i + ";";

                        query += "INSERT INTO bank_items (npc_id, player_id, item_id, slot, stack) VALUES (" +
                            npc_id + ", " +
                            player.PlayerID + ", " +
                            slot.Item.ItemID + ", " +
                            i + ", " +
                            slot.Stack + ");";
                    }

                    var command = new SqlCommand(query, world.SqlConnection);
                    command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
                }
            }
        }

        public ItemContainer GetOrCreateContainer(Player player, int npc_id)
        {
            ItemContainer container = null;
            if (!this.bankContainers.TryGetValue(npc_id, out container))
            {
                container = new ItemContainer(player.NumberOfBankPages * BankWindow.SlotsPerPage + 1);
                this.bankContainers[npc_id] = container;
            }

            return container;
        }

        public bool HasItem(int templateid)
        {
            foreach (var kvp in this.bankContainers)
            {
                foreach (var slot in kvp.Value)
                {
                    if (slot != null && slot.Item.Template.ID == templateid) return true;
                }
            }

            return false;
        }
    }
}
