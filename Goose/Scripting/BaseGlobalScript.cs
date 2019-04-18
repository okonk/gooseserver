using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class BaseGlobalScript : IGlobalScript
    {
        public BaseGlobalScript() { }

        public virtual void OnLoaded(GameWorld world)
        {

        }
    }
}
