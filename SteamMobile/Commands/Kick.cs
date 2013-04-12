using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Kick : Command
    {
        public override string Type { get { return "kick"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Ban) || parameters.Length < 1)
                return;

            try
            {
                var name = parameters[0];
                Program.Logger.InfoFormat("User '{0}' kicking '{1}'", target.Account.Name, name);

                string res;
                Program.Kick(name, out res);
                target.Send(res);
            }
            catch
            {
                target.Send("Failed to kick. Check logs.");
                throw;
            }
        }
    }
}
