using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    class QuestStarted
    {
        public int Id { get; set; }
        public Quest Quest { get; set; }
        public bool Dirty { get; set; }

        public static QuestStarted FromReader(SqlDataReader reader, GameWorld world)
        {
            var started = new QuestStarted();
            started.Id = Convert.ToInt32(reader["id"]);
            var questid = Convert.ToInt32(reader["quest_id"]);
            started.Quest = world.QuestHandler.Get(questid);
            started.Dirty = false;

            return started.Quest == null ? null : started;
        }
    }
}
