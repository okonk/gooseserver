using System.Text;

namespace Goose.Scripting
{
    public class ScriptHandler
    {
        private readonly GooseSettings settings;
        private readonly object gate = new();
        private Dictionary<string, IScript> scripts;

        public ScriptHandler(GooseSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.scripts = [];
        }

        public Script<T> GetScript<T>(string filePath)
        {
            filePath = this.settings.DataPathAbsolute + "/" + filePath;

            lock (this.gate)
            {
                if (this.scripts.TryGetValue(filePath, out var existing))
                    return (Script<T>)existing;
            }

            var script = new Script<T>(filePath);
            lock (this.gate)
            {
                if (this.scripts.TryGetValue(filePath, out var winner))
                    return (Script<T>)winner;

                this.scripts[filePath] = script;
                return script;
            }
        }

        public Script<T> CompileUnregistered<T>(string filePath)
        {
            return new Script<T>(this.settings.DataPathAbsolute + "/" + filePath);
        }

        public bool TryAdd<T>(Script<T> script)
        {
            lock (this.gate)
            {
                return this.scripts.TryAdd(script.FilePath, script);
            }
        }

        public Action PrepareReload()
        {
            KeyValuePair<string, IScript>[] snapshot;
            lock (this.gate)
            {
                snapshot = this.scripts.ToArray();
            }

            var commits = new List<Action>(snapshot.Length);
            foreach (var kvp in snapshot)
            {
                try
                {
                    commits.Add(kvp.Value.CompileForSwap());
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed loading {kvp.Key}: {e.Message}");
                }
            }

            return () =>
            {
                foreach (var commit in commits) commit();
            };
        }

        public bool HasScript(string filePath)
        {
            lock (this.gate)
            {
                return this.scripts.ContainsKey(filePath);
            }
        }
    }
}
