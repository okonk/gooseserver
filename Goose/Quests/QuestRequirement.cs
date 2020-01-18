using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    enum RequirementType
    {
        Gold,
        Item,
        Kill,
        TalkToNPC,
        ExperienceBanked,
        ExperienceSold,
        NothingEquipped,
    }

    class QuestRequirement
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

        public static QuestRequirement FromReader(DbDataReader reader)
        {
            var requirement = new QuestRequirement();

            requirement.Id = Convert.ToInt32(reader["id"]);
            requirement.Type = (RequirementType)Convert.ToInt32(reader["requirement_type"]);
            requirement.Value = Convert.ToInt64(reader["requirement_value"]);
            requirement.Value2 = Convert.ToInt64(reader["requirement_value2"]);
            requirement.KeepRequirement = ("0".Equals(Convert.ToString(reader["keep_requirement"])) ? false : true);

            return requirement;
        }
    }
}
