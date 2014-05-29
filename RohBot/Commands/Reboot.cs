using System;

namespace RohBot.Commands
{
    public class Reboot : Command
    {
        public override string Type { get { return "reboot"; } }

        public override string Format(CommandTarget target, string type) { return ""; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target))
                return;

            Program.Logger.Info("Reboot requested");
            Environment.Exit(0);
        }
    }
}
