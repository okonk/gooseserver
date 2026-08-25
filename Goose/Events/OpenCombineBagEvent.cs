using System.Text;

namespace Goose.Events
{
    /**
     * OpenCombineBagEvent, opens combine bag
     * 
     * Packet: OCB
     * 
     */
    class OpenCombineBagEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                CombineBagWindow.Open(world, this.Player);
            }
        }
    }
}
