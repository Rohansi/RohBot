using System;
using System.Linq;

namespace SteamMobile.Commands
{
    public class Name : Command
    {
        public override string Type
        {
            get { return "name"; }
        }

        public override string Format
        {
            get { return "]"; }
        }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession)
                return;

            var steamId = target.Session.AccountInfo.SteamId;
            if (steamId == "0")
                return;

            var lastChange = Util.DateTimeFromUnixTimestamp(target.Session.AccountInfo.LastNameChange);
            var timeSince = DateTime.UtcNow - lastChange;
            
            if (timeSince < TimeSpan.FromHours(1))
            {
                var timeLeft = TimeSpan.FromHours(1) - timeSince;
                target.Send(string.Format("You can only change your name once every hour. Try again in {0} minutes.", (int)timeLeft.TotalMinutes));
                return;
            }

            if (parameters.Length == 0)
            {
                target.Send("Missing name.");
                return;
            }

            var name = parameters[0];

            if (name.Length < 2 || name.Length > 24)
            {
                target.Send("Names must be between 2 and 24 characters long.");
                return;
            }

            if (!name.All(c => char.IsLetterOrDigit(c) || c == ' '))
            {
                target.Send("Names may only contain letters, digits and spaces.");
                return;
            }

            target.Session.AccountInfo.Name = parameters[0];
            target.Session.AccountInfo.LastNameChange = Util.GetCurrentUnixTimestamp();
            Database.AccountInfo.Save(target.Session.AccountInfo);

            target.Send("Your name has been changed.");
        }
    }
}
