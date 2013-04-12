using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Ban : Command
    {
        public override string Type { get { return "ban"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Ban) || parameters.Length < 1)
                return;

            try
            {
                var name = parameters[0];
                Program.Logger.InfoFormat("User '{0}' banning '{1}'", target.Account.Name, name);

                string res;
                Session.Ban(name.ToLower(), out res);
                target.Send(res);
            }
            catch
            {
                target.Send("Failed to ban. Check logs.");
                throw;
            }
        }
    }
}
