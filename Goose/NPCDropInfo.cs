using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    public class NPCDropInfo
    {
        public Decimal DropRate { get; set; }
        public int Stack { get; set; }
        public ItemTemplate ItemTemplate { get; set; }
    }
}
