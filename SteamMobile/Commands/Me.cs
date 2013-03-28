using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Me : Command
    {
        public override string Type { get { return "me"; }  }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession || parameters.Length < 1)
                return;

            var msg = target.Session.Name + " " + parameters[0];
            Program.MainChat.Send(msg);
        }
    }
}
