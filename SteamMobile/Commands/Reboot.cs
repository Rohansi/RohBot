using System;

namespace SteamMobile.Commands
{
    public class Reboot : Command
    {
        public override string Type
        {
            get { return "reboot"; }
        }

        public override string Format
        {
            get { return ""; }
        }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSteam || target.SteamId != Program.Settings.Admin)
                return;

            Program.Logger.Info("Reboot requested");
            Environment.Exit(0);
        }
    }
}
