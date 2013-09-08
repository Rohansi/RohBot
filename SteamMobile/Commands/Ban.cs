using System;

namespace SteamMobile.Commands
{
    public class Ban : Command
    {
        public override string Type { get { return "ban"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Admin) || parameters.Length < 1)
                return;

            try
            {
                var name = parameters[0];
                Program.Logger.InfoFormat("User '{0}' banning '{1}'", target.Account.Name, name);

                string res;
                Session.Ban(name.ToLower(), true, out res);
                target.Send(res);
                Program.Kick(name, out res);
            }
            catch (Exception e)
            {
                Program.Logger.Error("Ban", e);
                target.Send("Failed to ban. Check logs.");
                throw;
            }
        }
    }
}
