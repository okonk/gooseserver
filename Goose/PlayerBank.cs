using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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

        public int NumberOfContainers {  get { return bankContainers.Count; } }

        public Dictionary<int, ItemContainer> Containers { get { return bankContainers; } }

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
                    string serialized_data = Convert.ToString(reader["serialized_data"]);

                    ItemContainer container = GetOrCreateContainer(player, npc_id);

                    var containerSlots = JsonConvert.DeserializeObject<ItemSlot[]>(serialized_data, GameWorld.JsonSerializerSettings);
                    for (int i = 0; i < containerSlots.Length; i++)
                    {
                        var containerSlot = containerSlots[i];
                        if (containerSlot == null) continue;

                        world.ItemHandler.AddItem(containerSlot.Item, world);

                        containerSlot.Item.Template = world.ItemHandler.GetTemplate(containerSlot.Item.TemplateID);
                        containerSlot.Item.LoadTemplate(containerSlot.Item.Template);

                        container.SetSlot(i, containerSlots[i]);
                    }
                }
            }
        }

        public void Save(GameWorld world, Player player)
        {
            foreach (var kvp in this.bankContainers)
            {
                int npc_id = kvp.Key;
                ItemContainer container = kvp.Value;

                SqlCommand saveContainerCommand = new SqlCommand(
                    @"UPDATE bank_items SET serialized_data=@serialized_data WHERE npc_id=@npc_id AND player_id=@player_id; 
                    IF @@ROWCOUNT = 0 
                        INSERT INTO bank_items (npc_id, player_id, serialized_data) VALUES (@npc_id, @player_id, @serialized_data);", world.SqlConnection);
                saveContainerCommand.Parameters.Add(new SqlParameter("@npc_id", SqlDbType.Int) { Value = npc_id });
                saveContainerCommand.Parameters.Add(new SqlParameter("@player_id", SqlDbType.Int) { Value = player.PlayerID });
                saveContainerCommand.Parameters.Add(new SqlParameter("@serialized_data", SqlDbType.Text) { Value = JsonConvert.SerializeObject(container, GameWorld.JsonSerializerSettings) });
                world.DatabaseWriter.Add(saveContainerCommand);
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
