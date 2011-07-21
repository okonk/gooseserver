using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    public class NPCVendorSlot
    {
        public int Slot { get; set; }
        public ItemTemplate ItemTemplate { get; set; }
        public int Stack { get; set; }
        public bool CanSeeStats { get; set; }
    }
}
