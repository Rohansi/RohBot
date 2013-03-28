using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Users : Command
    {
        public override string Type { get { return "users"; }  }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession)
                return;

            var users = Program.MainChat.Members.Select(SteamName.Get).OrderBy(n => n);
            Program.SendMessage(target.Session, "*", "In this chat: " + string.Join(", ", users)); // TODO: send as packet
        }
    }
}
