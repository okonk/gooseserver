using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Text;

namespace Goose.Scripting
{
    public class Script<T> : IScript
    {
        public string FilePath { get; set; }

        public T Object { get; private set; } = default!;

        public Script(string filePath)
        {
            this.FilePath = filePath;

            LoadScript();
        }

        public void LoadScript()
        {
            if (!File.Exists(this.FilePath))
                throw new FileNotFoundException("Couldn't find script " + this.FilePath);

            string scriptContents = File.ReadAllText(this.FilePath);

            var scriptOptions = ScriptOptions.Default
                .WithReferences(
                    Assembly.GetExecutingAssembly(),
                    typeof(System.Text.Json.JsonSerializer).Assembly)
                .WithImports(
                    "System", "System.Collections.Generic", "System.Linq",
                    "System.Text.Json",
                    "Goose", "Goose.Events", "Goose.Quests", "Goose.Scripting")
                .WithFilePath(this.FilePath);

            var script = CSharpScript.Create(scriptContents, scriptOptions);
            script.Compile();

            var result = script.RunAsync().Result.ReturnValue;
            var scriptType = (Type)result;

            this.Object = (T)Activator.CreateInstance(scriptType)!;
        }
    }
}
