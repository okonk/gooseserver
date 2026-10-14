namespace Goose.CommunityGoals
{
    public class CommunityGoalDefinitionException : Exception
    {
        public CommunityGoalDefinitionException(string message) : base(message)
        {
        }

        public CommunityGoalDefinitionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
