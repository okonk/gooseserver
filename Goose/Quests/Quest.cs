using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace Goose.Quests
{
    public class Quest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FailText { get; set; }
        public string PassText { get; set; }
        public long ClassRestrictions { get; set; }
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
            this.Requirements = [];
            this.Rewards = [];
            this.PrerequisiteQuests = [];
        }

        public static Quest FromReader(DbDataReader reader, Dictionary<int, Quest> quests)
        {
            int id = reader.GetInt32("id");

            Quest quest = null;
            if (!quests.TryGetValue(id, out quest))
                quest = new Quest();

            quest.Id = id;
            quest.Name = reader.GetString("name");
            quest.Description = reader.GetString("description");
            quest.FailText = reader.GetString("fail_text");
            quest.PassText = reader.GetString("pass_text");
            quest.ClassRestrictions = reader.GetInt64("class_restrictions");
            quest.MinLevel = reader.GetInt32("min_level");
            quest.MaxLevel = reader.GetInt32("max_level");
            quest.MinExperience = reader.GetInt64("min_experience");
            quest.MaxExperience = reader.GetInt64("max_experience");
            quest.Repeatable = reader.GetString("repeatable") != "0";
            quest.ShowProgress = reader.GetString("show_progress") != "0";
            quest.OnlyOnePlayerCanComplete = reader.GetString("only_one_player_can_complete") != "0";
            quest.PrerequisiteQuests = reader.GetString("prerequisite_quests").Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Select(q => Convert.ToInt32(q)).ToList();

            return quest;
        }
    }
}
