using System.Text;

namespace Goose.Quests
{
    public class QuestHandler
    {
        public Dictionary<int, Quest> Quests { get; set; }

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
