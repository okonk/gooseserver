using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    class QuestProgress
    {
        public int Id { get; set; }
        public QuestRequirement Requirement { get; set; }
        public long Value { get; set; }
        public bool Dirty { get; set; }
        public bool Remove { get; set; }

        public static QuestProgress FromReader(SqlDataReader reader, GameWorld world, Player player)
        {
            var progress = new QuestProgress();
            progress.Id = Convert.ToInt32(reader["id"]);
            var requirementId = Convert.ToInt32(reader["requirement_id"]);
            var requirement = player.QuestsStarted.SelectMany(q => q.Quest.Requirements).Where(r => r.Id == requirementId).FirstOrDefault();

            if (requirement == null)
                return null;

            progress.Requirement = requirement;
            progress.Value = Convert.ToInt64(reader["progress_value"]);
            progress.Dirty = false;
            progress.Remove = false;

            return progress;
        }
    }
}
