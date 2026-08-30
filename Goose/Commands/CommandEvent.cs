using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Goose.Commands
{
    internal sealed class CommandEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly CommandDefinition _definition;
        private readonly string _packet;
        private readonly int _matchedLength;

        internal CommandEvent(CommandDefinition definition, string packet, int matchedLength)
        {
            this._definition = definition;
            this._packet = packet;
            this._matchedLength = matchedLength;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player is not { State: Player.States.Ready })
            {
                log.Debug("Dropped {0} for {1} (state {2}).",
                    this._definition.PrimaryKey, this.Player?.Name ?? "unknown",
                    this.Player is null ? "null" : this.Player.State.ToString());
                return;
            }

            var def = this._definition;
            // matchedLength comes from the trie match: alias keys can have different
            // lengths, so key lengths are not a valid cut point.
            var matchedKey = this._packet[..this._matchedLength];
            var remainder = this._packet.Substring(matchedKey.Length);
            var args = remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ctx = new CommandContext(this.Player, world, world.Commands, args, remainder);

            if (def.Instance is BaseCommand command
                && command.CheckAccessInternal(ctx, args) is { } privilege
                && !this.Player.HasPrivilege(privilege))
            {
                log.Debug("Refused {0} for {1}: missing {2}.", def.PrimaryKey, this.Player.Name, privilege);
                return;
            }

            MethodInfo? target = null;
            ParameterInfo[] parameters;
            ParameterInfo[] usageParameters;
            string? usageOverride;
            string usageKey;
            string[] targetTokens;

            SubcommandInfo? sub = null;
            if (def.Subcommands.Count > 0 && args.Length > 0)
                sub = def.Subcommands.FirstOrDefault(s => s.Names.Any(n =>
                    string.Equals(n, args[0], StringComparison.OrdinalIgnoreCase)));

            if (sub is { Privilege: not null } && !this.Player.HasPrivilege(sub.Privilege.Value))
            {
                if (def.ExecuteMethod is null)
                {
                    log.Debug("Refused {0} for {1}: missing {2}.",
                        sub.PrimaryName, this.Player.Name, sub.Privilege.Value);
                    return;
                }
                sub = null;
            }

            if (sub is not null)
            {
                target = sub.Method;
                parameters = sub.Parameters;
                usageParameters = sub.Parameters;
                usageOverride = sub.UsageOverride;
                usageKey = $"{def.PrimaryKey.TrimEnd()} {sub.PrimaryName}";
                targetTokens = args[1..];
            }
            else if (def.Handler is not null)
            {
                parameters = CommandBinder.InvocationParameters(def.Handler);
                usageParameters = CommandBinder.UsageParameters(def.Handler);
                usageOverride = def.UsageOverride;
                usageKey = def.PrimaryKey;
                targetTokens = args;
            }
            else if (def.ExecuteMethod is not null)
            {
                target = def.ExecuteMethod;
                parameters = target.GetParameters();
                usageParameters = parameters;
                usageOverride = def.UsageOverride;
                usageKey = def.PrimaryKey;
                targetTokens = args;
            }
            else
            {
                this.SendSubcommandList(ctx, def);
                return;
            }

            ctx.Usage = CommandBinder.Usage(usageKey, usageParameters, usageOverride);
            var (bound, error) = CommandBinder.Bind(world, this.Player, parameters, targetTokens, ctx.Usage);
            if (error is not null)
            {
                ctx.Send(error);
                return;
            }

            object?[] invocationArgs = [ctx, .. bound!];

            try
            {
                if (def.Handler is not null)
                    def.Handler.DynamicInvoke(invocationArgs);
                else
                    target!.Invoke(target.IsStatic ? null : def.Instance, invocationArgs);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException is not null)
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw;
            }
        }

        private void SendSubcommandList(CommandContext ctx, CommandDefinition def)
        {
            var lines = new List<string>();
            foreach (var sub in def.Subcommands)
            {
                if (sub.Privilege is not null && !this.Player.HasPrivilege(sub.Privilege.Value))
                    continue;
                lines.Add(sub.PrimaryName);
                lines.Add(CommandBinder.Usage($"{def.PrimaryKey.TrimEnd()} {sub.PrimaryName}", sub.Parameters, sub.UsageOverride));
                lines.Add(sub.Help);
            }
            if (lines.Count == 0)
                lines.Add("No subcommands.");
            ctx.Send(string.Join("\n", lines));
        }
    }
}
