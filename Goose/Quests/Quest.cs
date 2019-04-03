using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    class Quest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FailText { get; set; }
        public string PassText { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }
        public bool Repeatable { get; set; }
        public bool ShowProgress { get; set; }
        /// <summary>
        /// After the first player completes this quest, can anyone else complete it?
        /// Used for special one-time quests
        /// </summary>
        public bool OnlyOnePlayerCanComplete { get; set; }

        public List<int> PrerequisiteQuests { get; set; }

        public List<QuestRequirement> Requirements { get; set; }
        public List<QuestReward> Rewards { get; set; }

        public Quest()
        {
            this.Requirements = new List<QuestRequirement>();
            this.Rewards = new List<QuestReward>();
        }

        public static Quest FromReader(SqlDataReader reader, Dictionary<int, Quest> quests)
        {
            int id = Convert.ToInt32(reader["id"]);

            Quest quest = null;
            if (!quests.TryGetValue(id, out quest))
                quest = new Quest();

            quest.Id = id;
            quest.Name = Convert.ToString(reader["name"]);
            quest.Description = Convert.ToString(reader["description"]);
            quest.FailText = Convert.ToString(reader["fail_text"]);
            quest.PassText = Convert.ToString(reader["pass_text"]);
            quest.MinLevel = Convert.ToInt32(reader["min_level"]);
            quest.MaxLevel = Convert.ToInt32(reader["max_level"]);
            quest.MinExperience = Convert.ToInt64(reader["min_experience"]);
            quest.MaxExperience = Convert.ToInt64(reader["max_experience"]);
            quest.Repeatable = ("0".Equals(Convert.ToString(reader["repeatable"])) ? false : true);
            quest.ShowProgress = ("0".Equals(Convert.ToString(reader["show_progress"])) ? false : true);
            quest.OnlyOnePlayerCanComplete = ("0".Equals(Convert.ToString(reader["only_one_player_can_complete"])) ? false : true);
            quest.PrerequisiteQuests = Convert.ToString(reader["prerequisite_quests"]).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(q => Convert.ToInt32(q)).ToList();

            return quest;
        }
    }
}
