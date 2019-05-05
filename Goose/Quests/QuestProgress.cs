using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    class QuestProgress
    {
        public QuestRequirement Requirement { get; set; }
        public long Value { get; set; }
    }
}
