namespace Goose.Commands
{
    public abstract class BaseCommand
    {
        protected virtual AccessPrivilege? CheckAccess(CommandContext ctx, string[] args) => null;

        internal AccessPrivilege? CheckAccessInternal(CommandContext ctx, string[] args) => CheckAccess(ctx, args);
    }
}
