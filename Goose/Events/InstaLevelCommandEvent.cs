using System.Text;

namespace Goose.Events
{
    public class InstaLevelCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                //if (this.Player.Level < 50 && !this.Player.Class.ClassName.Equals("Commoner"))
                //{
                //    long exp = 4500000 - this.Player.Experience;
                //    this.Player.AddExperience((int)(exp / world.ExperienceModifier), world, Player.ExperienceMessage.Normal);
                //}
            }
        }
    }
}
