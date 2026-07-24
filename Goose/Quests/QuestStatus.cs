using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Goose.Quests
{
    /// <summary>
    /// Player's status for quests. Used for saving/loading from database
    /// </summary>
    public class QuestStatus
    {
        public class QuestProgress
        {
            [JsonPropertyName("id")]
            public int QuestId { get; set; }
            [JsonPropertyName("rid")]
            public int RequirementId { get; set; }
            [JsonPropertyName("p")]
            public long Progress { get; set; }

            public QuestProgress() { }

            public QuestProgress(int questId, int requirementId, long progress)
            {
                this.QuestId = questId;
                this.RequirementId = requirementId;
                this.Progress = progress;
            }
        }

        public int[] Started { get; set; }
        public int[] Completed { get; set; }
        public QuestProgress[] Progress { get; set; }
    }
}
