using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Text.Json;

namespace Goose.Quests
{
    public class QuestHandler
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public Dictionary<int, Quest> Quests { get; set; }

        public Dictionary<int, QuestClaim> Claims { get; } = [];

        public bool IsClaimed(int questId) => Claims.ContainsKey(questId);

        public bool TryGetClaim(int questId, out QuestClaim claim) => Claims.TryGetValue(questId, out claim!);

        public bool IsClaimedFor(Quest quest, Player player)
        {
            if (!quest.OnlyOnePlayerCanComplete) return false;
            if (!Claims.TryGetValue(quest.Id, out var claim)) return false;
            return !(claim.Roster.Contains(player.PlayerID)
                && !player.QuestsCompleted.Any(q => q.Id == quest.Id));
        }

        public void Claim(Quest quest, Player player, GameWorld world)
        {
            var claim = new QuestClaim
            {
                QuestId = quest.Id,
                PlayerId = player.PlayerID,
                CompletedAt = DateTime.UtcNow,
            };

            var roster = new List<Player>();
            foreach (var p in world.PlayerHandler.GetAllPlayerData())
            {
                if (p == player) continue;
                if (!QuestStateResolver.IsActive(quest, p)) continue;
                if (p.QuestsCompleted.Any(q => q.Id == quest.Id)) continue;
                if (!QuestStateResolver.MeetsRequirements(quest, p, world)) continue;
                roster.Add(p);
            }
            claim.Roster = [.. roster.Select(p => p.PlayerID)];
            Claims[quest.Id] = claim;

            var npcName = QuestWindow.FindGrantingNpc(world, quest.Id) ?? "the quest giver";
            foreach (var p in roster)
            {
                if (p.State == Player.States.Ready)
                    world.Send(p, P.ServerMessage($"You helped complete {quest.Name}. You can still turn it in at {npcName}."));
            }

            // OR REPLACE: a crash between claim and flush re-opens the quest, and re-claiming must not
            // hit the primary key. Runs on the game thread only; the dictionary is the source of truth.
            world.Database.Enqueue(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO quest_claims (quest_id, player_id, completed_at, player_ids) VALUES (@quest_id, @player_id, @completed_at, @player_ids)";
                command.Parameters.Add(new SQLiteParameter("@quest_id", DbType.Int32) { Value = claim.QuestId });
                command.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = claim.PlayerId });
                command.Parameters.Add(new SQLiteParameter("@completed_at", DbType.String) { Value = claim.CompletedAt.ToString("o") });
                command.Parameters.Add(new SQLiteParameter("@player_ids", DbType.String) { Value = JsonSerializer.Serialize(claim.Roster, JsonHelper.DatabaseOptions) });
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
            });
        }

        public void LoadClaims(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                this.Claims.Clear();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT quest_id, player_id, completed_at, player_ids FROM quest_claims";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var roster = new HashSet<int>();
                            var raw = reader.GetString("player_ids");
                            if (!string.IsNullOrEmpty(raw))
                            {
                                try { roster = JsonSerializer.Deserialize<HashSet<int>>(raw, JsonHelper.DatabaseOptions) ?? []; }
                                catch (JsonException e) { log.Error(e, "quest_claims roster for quest {0} is corrupt; starting empty", reader.GetInt32("quest_id")); }
                            }

                            this.Claims[reader.GetInt32("quest_id")] = new QuestClaim
                            {
                                QuestId = reader.GetInt32("quest_id"),
                                PlayerId = reader.GetInt32("player_id"),
                                CompletedAt = DateTime.Parse(reader.GetString("completed_at"), null, System.Globalization.DateTimeStyles.RoundtripKind),
                                Roster = roster,
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
