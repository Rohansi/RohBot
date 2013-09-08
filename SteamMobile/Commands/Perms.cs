using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Perms : Command
    {
        public override string Type
        {
            get { return "perms"; }
        }

        public override string Format
        {
            get { return "-]"; }
        }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Owner) || parameters.Length == 0)
                return;

            var account = Accounts.Find(parameters[0]);
            if (account == null)
            {
                target.Send("Account does not exist.");
                return;
            }

            if (parameters.Length == 1)
            {
                target.Send(string.Format("Permissions for {0}: {1}", account.Name, account.Permissions));
            }
            else
            {
                Permissions perms;
                if (!Enum.TryParse(parameters[1], true, out perms))
                {
                    target.Send("Invalid permissions.");
                    return;
                }

                perms &= ~Permissions.Owner;
                var flags = perms.ToString().Split(new[] {", "}, StringSplitOptions.RemoveEmptyEntries);
                var flagStr = string.Join(", ", flags.Where(f => !f.StartsWith("Unused")));
                Enum.TryParse(flagStr, true, out perms);

                account.Permissions = perms;
                account.Save();

                target.Send("Done.");
            }
        }
    }
}
