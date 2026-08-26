using System.Text;

namespace Goose.Scripting
{
    public class ScriptHandler
    {
        private readonly GooseSettings settings;
        private Dictionary<string, IScript> scripts;

        public ScriptHandler(GooseSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.scripts = [];
        }

        public Script<T> GetScript<T>(string filePath)
        {
            filePath = this.settings.DataPathAbsolute + "/" + filePath;

            IScript? script = null;
            if (!this.scripts.TryGetValue(filePath, out script))
            {
                script = new Script<T>(filePath);
                this.scripts[filePath] = script;
            }

            return (Script<T>)script;
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
                    throw new Exception($"Failed loading {kvp.Key}: {e.Message}");
                }
            }
        }

        public bool HasScript(string filePath)
        {
            return this.scripts.ContainsKey(filePath);
        }
    }
}
