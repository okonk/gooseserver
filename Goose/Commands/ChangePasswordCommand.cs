namespace Goose.Commands
{
    [Command("/changepassword ", Section = "General", Help = "Change your account password.")]
    public sealed class ChangePasswordCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] password)
        {
            string text = string.Join(" ", password);

            if (text.Length < 3)
            {
                ctx.Send("Your password needs to be more than 3 characters long.");
                return;
            }
            if (text.Length > 16)
            {
                ctx.Send("Your password needs to be 16 characters or fewer.");
                return;
            }

            ctx.Player.SetPassword(text);

            ctx.Send("Your password has been changed.");
        }
    }
}
