using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Goose
{
    public class PlayerBank
    {
        /// <summary>
        /// Maps an NPC ID to the ItemContainer containing the bank's items
        /// </summary>
        private Dictionary<int, ItemContainer> bankContainers;

        public int NumberOfContainers {  get => bankContainers.Count; }

        public Dictionary<int, ItemContainer> Containers { get => bankContainers; }

        public PlayerBank()
        {
            this.bankContainers = [];
        }

        public void Load(GameWorld world, Player player)
        {
            int playerId = player.PlayerID;
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM bank_items WHERE player_id=" + playerId;

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int npc_id = reader.GetInt32("npc_id");
                    string serialized_data = reader.GetString("serialized_data");

                    ItemContainer container = GetOrCreateContainer(player, npc_id, world.Settings.BankSlotsPerPage);

                    var containerSlots = JsonHelper.Deserialize<ItemSlot[]>(serialized_data)!;
                    for (int i = 0; i < containerSlots.Length; i++)
                    {
                        var containerSlot = containerSlots[i];
                        if (containerSlot is null) continue;

                        world.ItemHandler.AddItem(containerSlot.Item, world);

                        containerSlot.Item.Template = world.ItemHandler.GetTemplate(containerSlot.Item.TemplateID)!;
                        containerSlot.Item.RefreshStats();

                        container.SetSlot(i, containerSlots[i]);
                    }
                }
            });
        }

        public void Save(GameWorld world, Player player)
        {
            world.Database.Enqueue(this.BuildSave(player));
        }

        /**
         * BuildSave, snapshots state and returns the work to persist it
         *
         * Returned rather than enqueued so a full player save can run every part inside
         * one transaction.
         *
         */
        public Action<SQLiteConnection> BuildSave(Player player)
        {
            int playerId = player.PlayerID;
            // Snapshot container data before enqueue so later mutations don't race the DB thread.
            var snapshots = this.bankContainers
                .Select(kvp => (NpcId: kvp.Key, Json: JsonHelper.Serialize(kvp.Value)))
                .ToList();

            return conn =>
            {
                foreach (var (npcId, json) in snapshots)
                {
                    using var saveContainerCommand = conn.CreateCommand();
                    saveContainerCommand.CommandText =
                    @"INSERT INTO bank_items (npc_id, player_id, serialized_data) VALUES (@npc_id, @player_id, @serialized_data)
                      ON CONFLICT(npc_id, player_id) DO UPDATE SET serialized_data=@serialized_data WHERE npc_id=@npc_id AND player_id=@player_id;";
                    saveContainerCommand.Parameters.Add(new SQLiteParameter("@npc_id", DbType.Int32) { Value = npcId });
                    saveContainerCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = playerId });
                    saveContainerCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = json });
                    saveContainerCommand.ExecuteNonQuery();
                }
            };
        }

        public ItemContainer GetOrCreateContainer(Player player, int npc_id, int slotsPerPage)
        {
            ItemContainer? container = null;
            if (!this.bankContainers.TryGetValue(npc_id, out container))
            {
                container = new ItemContainer(player.NumberOfBankPages * slotsPerPage + 1);
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
                    if (slot is not null && slot.Item.Template.ID == templateid) return true;
                }
            }

            return false;
        }
    }
}
