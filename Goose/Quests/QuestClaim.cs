namespace Goose.Quests
{
    public class QuestClaim
    {
        public int QuestId { get; set; }
        public int PlayerId { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
