using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Goose.Quests
{
    public class QuestHandler
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public Dictionary<int, Quest> Quests { get; set; }

        public Dictionary<int, QuestClaim> Claims { get; } = [];

        public bool IsClaimed(int questId) => Claims.ContainsKey(questId);

        public bool TryGetClaim(int questId, out QuestClaim claim) => Claims.TryGetValue(questId, out claim!);

        public void Claim(Quest quest, Player player, GameWorld world)
        {
            Claims[quest.Id] = new QuestClaim
            {
                QuestId = quest.Id,
                PlayerId = player.PlayerID,
                CompletedAt = DateTime.UtcNow,
            };

            // OR REPLACE: a crash between claim and flush re-opens the quest, and re-claiming must not
            // hit the primary key. Runs on the game thread only; the dictionary is the source of truth.
            var claim = Claims[quest.Id];
            world.Database.Enqueue(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO quest_claims (quest_id, player_id, completed_at) VALUES (@quest_id, @player_id, @completed_at)";
                command.Parameters.Add(new SQLiteParameter("@quest_id", DbType.Int32) { Value = claim.QuestId });
                command.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = claim.PlayerId });
                command.Parameters.Add(new SQLiteParameter("@completed_at", DbType.String) { Value = claim.CompletedAt.ToString("o") });
                command.ExecuteNonQuery();
            }, e => { if (e is not null) log.Error(e, "Failed to persist quest claim {0}", quest.Id); });
        }

        public QuestHandler()
        {
            this.Quests = [];
        }

        public void LoadQuests(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM quests";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var quest = Quest.FromReader(reader, this.Quests);
                            this.Quests[quest.Id] = quest;
                        }
                    }
                }

                foreach (var quest in this.Quests.Values)
                {
                    var requirements = new List<QuestRequirement>();

                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM quest_requirements WHERE quest_id=" + quest.Id + " ORDER BY id";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var req = QuestRequirement.FromReader(reader, world, quest);
                                requirements.Add(req);
                            }
                        }
                    }

                    quest.Requirements = requirements;
                }

                foreach (var quest in this.Quests.Values)
                {
                    var rewards = new List<QuestReward>();

                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM quest_rewards WHERE quest_id=" + quest.Id + " ORDER BY id";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var reward = QuestReward.FromReader(reader, world, quest);
                                rewards.Add(reward);
                            }
                        }
                    }

                    quest.Rewards = rewards;
                }

                this.Claims.Clear();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT quest_id, player_id, completed_at FROM quest_claims";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            this.Claims[reader.GetInt32("quest_id")] = new QuestClaim
                            {
                                QuestId = reader.GetInt32("quest_id"),
                                PlayerId = reader.GetInt32("player_id"),
                                CompletedAt = DateTime.Parse(reader.GetString("completed_at"), null, System.Globalization.DateTimeStyles.RoundtripKind),
                            };
                        }
                    }
                }
            });
        }

        public Quest? Get(int questId)
        {
            Quest? quest = null;

            if (this.Quests.TryGetValue(questId, out quest))
            {
                return quest;
            }

            return null;
        }

        /// <summary>Registers a script-generated quest. Overwrites any existing entry with the same id.</summary>
        public void AddQuest(Quest quest)
        {
            this.Quests[quest.Id] = quest;
        }

        public void SendIcon(Player viewer, NPC npc, GameWorld world)
        {
            switch (QuestStateResolver.Resolve(npc, viewer, world))
            {
                case QuestIconState.Ready:
                    world.SendCharacterIcon(viewer, npc, world.Settings.QuestReadyIconSheet, world.Settings.QuestReadyIconGraphic);
                    break;
                case QuestIconState.Available:
                    world.SendCharacterIcon(viewer, npc, world.Settings.QuestAvailableIconSheet, world.Settings.QuestAvailableIconGraphic);
                    break;
                default:
                    // Always send, even clear: a stale icon must not survive a quest ending.
                    world.SendCharacterIcon(viewer, npc, 0, 0);
                    break;
            }
        }

        public void RefreshIcons(Player viewer, GameWorld world)
        {
            if (viewer.State != Player.States.Ready || viewer.Map is null)
                return;

            foreach (var npc in viewer.Map.GetNPCsInRange(viewer))
                SendIcon(viewer, npc, world);
        }
    }
}
