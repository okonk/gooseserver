using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Goose.Commands
{
    public sealed class CommandRegistry
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly object _gate = new();
        private volatile CommandSnapshot _snapshot = new();


        public void SeedBuiltins()
        {
            this.SeedAttributedTypes(typeof(GameWorld).Assembly.GetTypes());
        }

        public bool Register(string key, string section, string help, Delegate handler)
            => this.RegisterKeys([key], null, section, help, handler);

        public bool Register(string key, AccessPrivilege privilege, string section, string help, Delegate handler)
            => this.RegisterKeys([key], privilege, section, help, handler);

        internal void SeedAttributedTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<CommandAttribute>();
                if (attribute is null || !typeof(BaseCommand).IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                try
                {
                    this.SeedAttributedType(type, attribute);
                }
                catch (Exception e)
                {
                    log.Error(e, "Rejecting command type {0}.", type.FullName);
                }
            }
        }

        private void SeedAttributedType(Type type, CommandAttribute attribute)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var executes = methods.Where(m => m.Name == "Execute" && !m.IsSpecialName).ToList();
            var subcommands = methods
                .Where(m => m.GetCustomAttribute<SubcommandAttribute>() is not null)
                .ToList();

            if (executes.Count > 1)
            {
                log.Error("Rejecting {0}: more than one Execute method.", type.FullName);
                return;
            }
            if (executes.Count == 0 && subcommands.Count == 0)
            {
                log.Error("Rejecting {0}: no Execute or [Subcommand] methods.", type.FullName);
                return;
            }
            if (!CommandBinder.IsValidKey(attribute.Key))
            {
                log.Error("Rejecting {0}: invalid key {1}.", type.FullName, attribute.Key);
                return;
            }
            var targets = new List<MethodInfo>(subcommands);
            if (executes.Count == 1)
                targets.Insert(0, executes[0]);

            foreach (var method in targets)
            {
                if (!CommandBinder.IsValidTarget(method.GetParameters(), out var error))
                {
                    log.Error("Rejecting {0}.{1}: {2}.", type.FullName, method.Name, error);
                    return;
                }
            }

            var instance = Activator.CreateInstance(type) as BaseCommand;
            if (instance is null)
            {
                log.Error("Rejecting {0}: could not instantiate.", type.FullName);
                return;
            }

            var infos = subcommands.Select(m =>
            {
                var sub = m.GetCustomAttribute<SubcommandAttribute>()!;
                return new SubcommandInfo(sub.Name, [sub.Name], m, m.GetParameters(), sub.Help, sub.Privilege, sub.Usage);
            }).ToList();

            this.RegisterAttributed([attribute.Key], attribute.Privilege, attribute.Section, attribute.Help,
                attribute.Usage, instance, executes.Count == 1 ? executes[0] : null, infos, type.FullName ?? type.ToString());
        }

        internal bool RegisterAttributed(string[] keys, AccessPrivilege? privilege, string? section, string help,
            string? usageOverride, object instance, MethodInfo? executeMethod, List<SubcommandInfo> subcommands,
            string logContext)
        {
            lock (this._gate)
            {
                var snapshot = this._snapshot;
                foreach (var key in keys)
                {
                    if (snapshot.ByKey.ContainsKey(key))
                    {
                        log.Error("Rejecting {0}: key {1} is already registered.", logContext, key);
                        return false;
                    }
                }
                return this.Publish(keys, privilege, section, help, usageOverride,
                    () => new CommandDefinition(keys, privilege, section, help, usageOverride,
                        instance, null, executeMethod, subcommands, null, null));
            }
        }

        internal bool RegisterLegacy(string key, Type eventType, AccessPrivilege? privilege)
            => this.RegisterLegacy(key, privilege, eventType, null);

        internal bool RegisterLegacy(string key, AccessPrivilege? privilege, Type? eventType, EventHandler.CreateEvent? factory)
        {
            lock (this._gate)
            {
                if (eventType is null && factory is null)
                {
                    log.Error("Rejecting legacy command {0}: no event type or factory.", key);
                    return false;
                }
                var snapshot = this._snapshot;
                if (snapshot.ByKey.ContainsKey(key))
                {
                    log.Error("Rejecting legacy command {0}: key already registered.", key);
                    return false;
                }
                return this.Publish([key], privilege, null, "", null,
                    () => new CommandDefinition([key], privilege, null, "", null,
                        null, factory, null, [], factory, eventType),
                    requireHelp: false);
            }
        }

        internal bool RegisterKeys(string[] keys, AccessPrivilege? privilege, string? section, string help, Delegate handler)
        {
            lock (this._gate)
            {
                if (handler is null)
                {
                    log.Error("Refusing to register: handler is null.");
                    return false;
                }
                if (!CommandBinder.IsValidTarget(CommandBinder.InvocationParameters(handler), out var error))
                {
                    log.Error("Refusing to register {0}: {1}.", keys.Length > 0 ? keys[0] : "?", error);
                    return false;
                }
                return this.Publish(keys, privilege, section, help, null,
                    () => new CommandDefinition(keys, privilege, section, help, null,
                        null, handler, null, [], null, null));
            }
        }

        internal CommandSnapshot Snapshot => this._snapshot;

        internal bool TryGet(string key, [MaybeNullWhen(false)] out CommandDefinition definition)
        {
            var snapshot = this._snapshot;
            return snapshot.ByKey.TryGetValue(key, out definition);
        }

        internal IReadOnlyList<CommandSection> Sections
            => this.SectionsOf(this._snapshot);

        internal IReadOnlyList<CommandSection> SectionsOf(CommandSnapshot snapshot)
        {
            var sections = new List<CommandSection>();
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var def in snapshot.Ordered)
            {
                if (def.Section is null)
                    continue;
                if (!index.TryGetValue(def.Section, out var i))
                {
                    i = sections.Count;
                    index[def.Section] = i;
                    sections.Add(new CommandSection(def.Section));
                }
                sections[i].Commands.Add(def);
            }

            return sections;
        }

        internal IReadOnlyList<string> FindNameCollisions()
            => this.Collisions(this._snapshot);

        internal static bool IsUsableBy(Player player, CommandDefinition def)
            => def.Privilege is null || player.HasPrivilege(def.Privilege.Value);

        private List<string> Collisions(CommandSnapshot snapshot)
        {
            var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in snapshot.Ordered)
                if (def.Section is not null)
                    sections.Add(def.Section);

            var collisions = new List<string>();
            foreach (var def in snapshot.Ordered)
            {
                var name = def.PrimaryKey.Trim().TrimStart('/');
                foreach (var section in sections)
                    if (string.Equals(section, name, StringComparison.OrdinalIgnoreCase) && !collisions.Contains(section))
                        collisions.Add(section);
            }
            return collisions;
        }

        // One lock for all mutations; each publish builds fresh Trie/Dictionary/List
        // instances so a published snapshot is never mutated afterwards.
        private bool Publish(string[] keys, AccessPrivilege? privilege, string? section, string help,
            string? usageOverride, Func<CommandDefinition> factory, bool requireHelp = true)
        {
            if (keys is null || keys.Length == 0)
            {
                log.Error("Refusing to register: no keys.");
                return false;
            }
            foreach (var key in keys)
            {
                if (key is null || !CommandBinder.IsValidKey(key))
                {
                    log.Error("Refusing to register {0}: invalid key.", key ?? "(null)");
                    return false;
                }
            }
            if (requireHelp && string.IsNullOrEmpty(help))
            {
                log.Error("Refusing to register {0}: empty help.", keys[0]);
                return false;
            }
            if (keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            {
                log.Error("Refusing to register: duplicate keys in request.");
                return false;
            }

            var snapshot = this._snapshot;
            var replaced = new List<CommandDefinition>();
            foreach (var key in keys)
                if (snapshot.ByKey.TryGetValue(key, out var existing) && !replaced.Contains(existing))
                    replaced.Add(existing);

            if (replaced.Count > 1)
            {
                log.Error("Refusing to register {0}: keys belong to {1} existing definitions.", keys[0], replaced.Count);
                return false;
            }

            var replacedDef = replaced.Count == 1 ? replaced[0] : null;
            if (replacedDef is not null && replacedDef.Privilege is not null && privilege is null)
            {
                log.Error("Refusing to register {0} unprivileged: it already requires {1}.",
                    keys[0], replacedDef.Privilege.Value);
                return false;
            }

            var def = factory();

            var insertAt = -1;
            if (replacedDef is not null)
            {
                for (var i = 0; i < snapshot.Ordered.Count; i++)
                    if (snapshot.Ordered[i] == replacedDef)
                    {
                        insertAt = i;
                        break;
                    }
            }
            var ordered = new List<CommandDefinition>(snapshot.Ordered.Count + (replacedDef is null ? 1 : 0));
            for (var i = 0; i < snapshot.Ordered.Count; i++)
            {
                if (i == insertAt)
                    ordered.Add(def);
                if (snapshot.Ordered[i] != replacedDef)
                    ordered.Add(snapshot.Ordered[i]);
            }
            if (replacedDef is null)
                ordered.Add(def);

            var byKey = new Dictionary<string, CommandDefinition>(StringComparer.Ordinal);
            foreach (var (key, existing) in snapshot.ByKey)
                if (existing != replacedDef)
                    byKey[key] = existing;
            foreach (var key in keys)
                byKey[key] = def;

            var trie = new Trie<CommandDefinition>();
            foreach (var (key, existing) in byKey)
                trie.Insert(key, existing);

            var published = new CommandSnapshot(trie, byKey, ordered);
            this._snapshot = published;

            foreach (var name in this.Collisions(published))
                log.Warn("Command name {0} collides with a section name.", name);

            return true;
        }
    }

    internal sealed class CommandSnapshot
    {
        public Trie<CommandDefinition> Trie { get; }
        public IReadOnlyDictionary<string, CommandDefinition> ByKey { get; }
        public IReadOnlyList<CommandDefinition> Ordered { get; }

        internal CommandSnapshot()
            : this(new Trie<CommandDefinition>(), new Dictionary<string, CommandDefinition>(),
                Array.Empty<CommandDefinition>())
        {
        }

        internal CommandSnapshot(Trie<CommandDefinition> trie,
            IReadOnlyDictionary<string, CommandDefinition> byKey,
            IReadOnlyList<CommandDefinition> ordered)
        {
            this.Trie = trie;
            this.ByKey = byKey;
            this.Ordered = ordered;
        }
    }

    internal sealed class CommandSection
    {
        public string Name { get; }
        public List<CommandDefinition> Commands { get; }

        internal CommandSection(string name)
        {
            this.Name = name;
            this.Commands = [];
        }
    }
}
