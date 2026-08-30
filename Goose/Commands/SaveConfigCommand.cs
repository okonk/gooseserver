namespace Goose.Commands
{
    [Command("/saveconfig", AccessPrivilege.SetConfig, Section = "Admin", Help = "Save game settings.")]
    public sealed class SaveConfigCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            // Commented out because this is bad, it saves the settings to some random path in appdata
            //ctx.World.Settings.Save();
            //ctx.Send("$7Game Settings Saved.");
        }
    }
}
