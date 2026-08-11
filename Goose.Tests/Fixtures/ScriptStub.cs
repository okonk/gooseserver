using System.Runtime.CompilerServices;
using Goose.Scripting;

namespace Goose.Tests;

/// <summary>Wraps an in-memory script object in a Script&lt;T&gt; without touching disk.
/// Script&lt;T&gt;'s constructor compiles a file (Script.cs:26), and Object has a private
/// setter (Script.cs:17), so the instance is allocated uninitialised and the backing
/// property is set by reflection.</summary>
public static class ScriptStub
{
    public static Script<T> For<T>(T instance)
    {
        var script = (Script<T>)RuntimeHelpers.GetUninitializedObject(typeof(Script<T>));
        typeof(Script<T>).GetProperty(nameof(Script<T>.Object))!
            .SetValue(script, instance);
        return script;
    }
}
