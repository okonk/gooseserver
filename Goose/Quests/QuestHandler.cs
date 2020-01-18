using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    class QuestHandler
    {
        public Dictionary<int, Quest> Quests { get; set; }

        public QuestHandler()
        {
            this.Quests = new Dictionary<int, Quest>();
        }

        public void LoadQuests(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM quests";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var quest = Quest.FromReader(reader, this.Quests);
                    this.Quests[quest.Id] = quest;
                }
            }

            foreach (var quest in this.Quests.Values)
            {
                var requirements = new List<QuestRequirement>();

                command = world.SqlConnection.CreateCommand();
                command.CommandText = "SELECT * FROM quest_requirements WHERE quest_id=" + quest.Id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var req = QuestRequirement.FromReader(reader);
                        req.Quest = quest;
                        requirements.Add(req);
                    }
                }

                quest.Requirements = requirements;
            }

            foreach (var quest in this.Quests.Values)
            {
                var rewards = new List<QuestReward>();

                command = world.SqlConnection.CreateCommand();
                command.CommandText = "SELECT * FROM quest_rewards WHERE quest_id=" + quest.Id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var reward = QuestReward.FromReader(reader);
                        rewards.Add(reward);
                    }
                }

                quest.Rewards = rewards;
            }
        }

        public Quest Get(int questId)
        {
            Quest quest = null;

            if (this.Quests.TryGetValue(questId, out quest))
            {
                return quest;
            }

            return null;
        }
    }
}
