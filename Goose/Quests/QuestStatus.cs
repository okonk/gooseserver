using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            [JsonProperty(PropertyName = "id")]
            public int QuestId { get; set; }
            [JsonProperty(PropertyName = "rid")]
            public int RequirementId { get; set; }
            [JsonProperty(PropertyName = "p")]
            public long Progress { get; set; }

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
