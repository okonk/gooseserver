using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    public class WarpTile : ITile
    {
        public Map WarpMap { get; set; }
        public int WarpX { get; set; }
        public int WarpY { get; set; }
    }
}
