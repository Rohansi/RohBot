using System;

namespace SteamMobile.Commands
{
    public class Reboot : Command
    {
        public override string Type { get { return "reboot"; } }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Admin))
                return;

            Program.Exit("Reboot requested");
        }
    }
}
