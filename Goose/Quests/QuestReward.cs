using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    enum RewardType
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
    }

    class QuestReward
    {
        public int Id { get; set; }
        public RewardType Type { get; set; }
        public long LongValue { get; set; }
        public long LongValue2 { get; set; }
        public string StringValue { get; set; }

        public static QuestReward FromReader(DbDataReader reader)
        {
            var reward = new QuestReward();

            reward.Id = Convert.ToInt32(reader["id"]);
            reward.Type = (RewardType)Convert.ToInt32(reader["reward_type"]);
            reward.LongValue = Convert.ToInt64(reader["long_value"]);
            reward.LongValue2 = Convert.ToInt64(reader["long_value2"]);
            reward.StringValue = Convert.ToString(reader["string_value"]);

            return reward;
        }
    }
}
