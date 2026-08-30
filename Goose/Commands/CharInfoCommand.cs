namespace Goose.Commands
{
    [Command("/charinfo", Section = "General", Help = "Open your character info window.")]
    public sealed class CharInfoCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;

            foreach (var window in ctx.Player.Windows)
            {
                if (window.Type == Window.WindowTypes.CharInfo)
                {
                    window.Refresh(ctx.Player, world);
                    return;
                }
            }

            Window w = new Window();
            w.Title = "Character Info:";
            w.Type = Window.WindowTypes.CharInfo;
            w.Buttons = "0,0,0,0,0";

            ctx.Player.Windows.Add(w);
            w.Create(ctx.Player, world);
        }
    }
}
