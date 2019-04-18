using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class ScriptHandler
    {
        private Dictionary<string, IScript> scripts;

        public ScriptHandler()
        {
            this.scripts = new Dictionary<string, IScript>();
        }

        public Script<T> GetScript<T>(string filePath)
        {
            IScript script = null;
            if (!this.scripts.TryGetValue(filePath, out script))
            {
                script = LoadScript<T>(filePath);
            }

            return (Script<T>)script;
        }

        public IScript LoadScript<T>(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Couldn't find script " + filePath);

            var script = new Script<T>(filePath);
            this.scripts[filePath] = script;

            return script;
        }

        public void ReloadScripts()
        {
            foreach (var kvp in scripts)
            {
                try
                {
                    kvp.Value.LoadScript();
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Failed loading {0}: {1}", kvp.Key, e.Message));
                }
            }
        }

        public bool HasScript(string filePath)
        {
            return this.scripts.ContainsKey(filePath);
        }
    }
}
