using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Refresh : Command
    {
        public override string Type { get { return "refresh"; } }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Admin))
                return;

            Settings.Reload();
            Accounts.Reload();

            target.Send("Done.");
        }
    }
}
