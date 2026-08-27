using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Text.Json;

namespace Goose
{
    public class PlayerBank
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

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

                    if (string.IsNullOrEmpty(serialized_data))
                    {
                        log.Error("player {0}: bank row for npc {1} is empty; skipped", playerId, npc_id);
                        continue;
                    }

                    ItemSlot[]? containerSlots = null;
                    try
                    {
                        containerSlots = JsonHelper.Deserialize<ItemSlot[]>(serialized_data);
                    }
                    catch (JsonException e)
                    {
                        log.Error("player {0}: bank blob for npc {1} is corrupt; skipped", playerId, npc_id, e);
                    }

                    if (containerSlots is null)
                    {
                        log.Warn("player {0}: no bank data for npc {1}; skipped", playerId, npc_id);
                        continue;
                    }

                    for (int i = 0; i < containerSlots.Length && i < container.MaxSlots; i++)
                    {
                        var containerSlot = containerSlots[i];
                        if (containerSlot is null) continue;

                        if (containerSlot.Item is null)
                        {
                            log.Error("player {0}: slot with null item discarded", playerId);
                            continue;
                        }

                        ItemTemplate? template = world.ItemHandler.GetTemplate(containerSlot.Item.TemplateID);
                        if (template is null)
                        {
                            log.Error("player {0}: item template {1} not found; slot discarded", playerId, containerSlot.Item.TemplateID);
                            continue;
                        }

                        world.ItemHandler.AddItem(containerSlot.Item, world);
                        containerSlot.Item.Template = template;
                        containerSlot.Item.RefreshStats();

                        container.SetSlot(i, containerSlot);
                    }

                    if (containerSlots.Length > container.MaxSlots)
                        log.Warn("player {0}: bank blob for npc {1} has {2} slots, container holds {3}; excess discarded", playerId, npc_id, containerSlots.Length, container.MaxSlots);
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
