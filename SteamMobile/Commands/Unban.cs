using System;

namespace SteamMobile.Commands
{
    public class Unban : Command
    {
        public override string Type { get { return "unban"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Ban) || parameters.Length < 1)
                return;

            try
            {
                var name = parameters[0];
                Program.Logger.InfoFormat("User '{0}' unbanning '{1}'", target.Account.Name, name);

                string res;
                Session.Ban(name.ToLower(), false, out res);
                target.Send(res);
            }
            catch (Exception e)
            {
                Program.Logger.Error("Unban", e);
                target.Send("Failed to unban. Check logs.");
                throw;
            }
        }
    }
}
