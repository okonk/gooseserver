using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface IGlobalScript
    {
        void OnLoaded(GameWorld world);
    }
}
