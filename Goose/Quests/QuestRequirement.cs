using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Goose.Scripting;

namespace Goose.Quests
{
    public enum RequirementType
    {
        Gold,
        Item,
        Kill,
        TalkToNPC,
        ExperienceBanked,
        ExperienceSold,
        NothingEquipped,
        Script,          // 7 — logic lives in script_path
    }

    public class QuestRequirement
    {
        public int Id { get; set; }
        public RequirementType Type { get; set; }
        public Quest Quest { get; set; }
        public long Value { get; set; }
        public long Value2 { get; set; }
        /// <summary>
        /// when the player completes the quest, is the requirement kept, or removed?
        /// for example, taking the required item, gold
        /// maybe take required exp, etc.
        /// </summary>
        public bool KeepRequirement { get; set; }
        public Script<IQuestScript> Script { get; set; }
        public string ScriptParams { get; set; }

        public static QuestRequirement FromReader(DbDataReader reader, GameWorld world, Quest quest)
        {
            var requirement = new QuestRequirement();
            requirement.Quest = quest;

            requirement.Id = Convert.ToInt32(reader["id"]);
            requirement.Type = (RequirementType)Convert.ToInt32(reader["requirement_type"]);
            requirement.Value = Convert.ToInt64(reader["requirement_value"]);
            requirement.Value2 = Convert.ToInt64(reader["requirement_value2"]);
            requirement.KeepRequirement = ("0".Equals(Convert.ToString(reader["keep_requirement"])) ? false : true);

            requirement.ScriptParams = Convert.ToString(reader["script_params"]);
            string scriptPath = Convert.ToString(reader["script_path"]);
            if (!string.IsNullOrEmpty(scriptPath))
            {
                requirement.Script = world.ScriptHandler.GetScript<IQuestScript>(scriptPath);
            }

            if (requirement.Type == RequirementType.Script && requirement.Script == null)
            {
                throw new Exception($"Quest requirement {requirement.Id} (quest {quest.Id}) has type Script but no script_path");
            }

            return requirement;
        }
    }
}
