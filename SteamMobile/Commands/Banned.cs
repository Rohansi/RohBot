using System;
using System.Linq;

namespace SteamMobile.Commands
{
    public class Banned : Command
    {
        public override string Type { get { return "banned"; } }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            var banned = Accounts.List().Where(a => a.Banned).ToList();

            if (banned.Count == 0)
            {
                target.Send("Nobody is banned.");
                return;
            }

            target.Send("Banned Accounts: " + string.Join(", ", banned.Select(a => a.Name)));
        }
    }
}
