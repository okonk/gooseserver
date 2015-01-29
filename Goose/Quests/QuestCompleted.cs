using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    class QuestCompleted
    {
        public int Id { get; set; }
        public Quest Quest { get; set; }
        public bool Dirty { get; set; }

        public static QuestCompleted FromReader(SqlDataReader reader, GameWorld world)
        {
            var completed = new QuestCompleted();
            completed.Id = Convert.ToInt32(reader["id"]);
            var questid = Convert.ToInt32(reader["quest_id"]);
            completed.Quest = world.QuestHandler.Get(questid);
            completed.Dirty = false;

            return completed.Quest == null ? null : completed;
        }
    }
}
