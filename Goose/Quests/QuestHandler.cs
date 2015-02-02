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
            SqlCommand command = new SqlCommand("SELECT * FROM quests", world.SqlConnection);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var quest = Quest.FromReader(reader);
                    this.Quests[quest.Id] = quest;
                }
            }

            foreach (var quest in this.Quests.Values)
            {
                command = new SqlCommand("SELECT * FROM quest_requirements WHERE quest_id=" + quest.Id, world.SqlConnection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var req = QuestRequirement.FromReader(reader);
                        req.Quest = quest;
                        quest.Requirements.Add(req);
                    }
                }
            }

            foreach (var quest in this.Quests.Values)
            {
                command = new SqlCommand("SELECT * FROM quest_rewards WHERE quest_id=" + quest.Id, world.SqlConnection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var reward = QuestReward.FromReader(reader);
                        quest.Rewards.Add(reward);
                    }
                }
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
