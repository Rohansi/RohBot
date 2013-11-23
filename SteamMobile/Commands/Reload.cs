
namespace SteamMobile.Commands
{
    public class Reload : Command
    {
        public override string Type { get { return "reload"; } }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target))
                return;

            Program.LoadSettings();
            target.Send("Done.");
        }
    }
}
