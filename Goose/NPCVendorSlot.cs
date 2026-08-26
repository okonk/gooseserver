using System.Text;

namespace Goose
{
    public class NPCVendorSlot
    {
        public int Slot { get; set; }
        public ItemTemplate ItemTemplate { get; set; } = null!;
        public int Stack { get; set; }
        public bool CanSeeStats { get; set; }
    }
}
