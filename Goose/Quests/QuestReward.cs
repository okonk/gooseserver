using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Goose.Scripting;

namespace Goose.Quests
{
    public enum RewardType
    {
        Gold,
        Item,
        Title,
        Surname,
        Teleport,
        Experience,
        FaceGraphic,
        BodyGraphic,
        HairGraphic,
        HairColour,
        BodyColour,
        ClassChange,
        HP,
        MP,
        AC,
        Stamina,
        Strength,
        Dexterity,
        Intelligence,
        SpellBuff,
        LearnSpell,
        Script,          // 21 — logic lives in script_path
    }

    public class QuestReward
    {
        public int Id { get; set; }
        public RewardType Type { get; set; }
        public long LongValue { get; set; }
        public long LongValue2 { get; set; }
        public string StringValue { get; set; }
        public Script<IQuestScript> Script { get; set; }
        public string ScriptParams { get; set; }

        public static QuestReward FromReader(DbDataReader reader, GameWorld world, Quest quest)
        {
            var reward = new QuestReward();

            reward.Id = Convert.ToInt32(reader["id"]);
            reward.Type = (RewardType)Convert.ToInt32(reader["reward_type"]);
            reward.LongValue = Convert.ToInt64(reader["long_value"]);
            reward.LongValue2 = Convert.ToInt64(reader["long_value2"]);
            reward.StringValue = Convert.ToString(reader["string_value"]);

            reward.ScriptParams = Convert.ToString(reader["script_params"]);
            string scriptPath = Convert.ToString(reader["script_path"]);
            if (!string.IsNullOrEmpty(scriptPath))
            {
                reward.Script = world.ScriptHandler.GetScript<IQuestScript>(scriptPath);
            }

            if (reward.Type == RewardType.Script && reward.Script == null)
            {
                throw new Exception($"Quest reward {reward.Id} (quest {quest.Id}) has type Script but no script_path");
            }

            return reward;
        }
    }
}
