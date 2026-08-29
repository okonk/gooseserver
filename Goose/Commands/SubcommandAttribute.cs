using System;

namespace Goose.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubcommandAttribute : Attribute
    {
        public string Name { get; }
        public AccessPrivilege? Privilege { get; }
        public string Help { get; set; } = null!;
        public string? Usage { get; set; }

        public SubcommandAttribute(string name) { Name = name; }
        public SubcommandAttribute(string name, AccessPrivilege privilege) { Name = name; Privilege = privilege; }
    }
}
