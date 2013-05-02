using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EzSteam;

namespace SteamMobile.Commands
{
    public class Rejoin : Command
    {
        public override string Type { get { return "rejoin"; } }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Ban) || Program.MainChat == null)
                return;

            Program.Logger.InfoFormat("User '{0}' requesting rejoin.", target.Account.Name);
            Program.MainChat.Leave(Chat.LeaveReason.Left);
        }
    }
}
