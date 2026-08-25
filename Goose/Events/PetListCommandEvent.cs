using System.Text;

namespace Goose.Events
{
    /// <summary>
    /// Command that lists all of the player's current pets
    /// </summary>
    public class PetListCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, P.ServerMessage("Listing Pets: <ID> <Name> <Level>"));

                foreach (Pet pet in this.Player.Pets)
                {
                    world.Send(this.Player, P.ServerMessage(pet.PetID + " " + pet.Name + " " + pet.Level));
                }
            }
        }
    }
}
