using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Goose.Scripting
{
    public class Script<T> : IScript
    {
        public string FilePath { get; set; }

        public T Object { get; private set; }

        public Script(string filePath)
        {
            this.FilePath = filePath;

            LoadScript();
        }

        public void LoadScript()
        {
            string scriptContents = File.ReadAllText(this.FilePath);
            var script = CSharpScript.Create(scriptContents, ScriptOptions.Default.WithReferences(Assembly.GetExecutingAssembly(), typeof(JsonConvert).GetType().Assembly));
            script.Compile();

            var result = script.RunAsync().Result.ReturnValue;
            var scriptType = (Type)result;

            this.Object = (T)Activator.CreateInstance(scriptType);
        }
    }
}
