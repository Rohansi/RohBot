
namespace RohBot.Commands
{
    public class Ban : Command
    {
        public override string Type => "ban";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !Util.IsMod(target) || parameters.Length == 0)
                return;

            if (target.Room.DisableBanning && !Util.IsAdmin(target))
                return;

            if (!Util.IsValidUsername(parameters[0]))
            {
                target.Send("Invalid username.");
                return;
            }

            var forAccount = Account.Get(parameters[0]);

            if (forAccount == null)
            {
                target.Send("Account does not exist.");
                return;
            }

            if (Util.IsAdmin(target.Room, forAccount.Name))
            {
                target.Send("Administrators can not be banned.");
                return;
            }

            if (target.Room.IsBanned(forAccount.Name))
            {
                target.Send("Account is already banned.");
                return;
            }

            var line = new StateLine
            {
                Date = Util.GetCurrentTimestamp(),
                Chat = target.Room.RoomInfo.ShortName,
                State = "Banned",
                For = forAccount.Name,
                ForId = forAccount.Id.ToString("D"),
                ForType = "RohBot",
                ForStyle = forAccount.EnabledStyle
            };

            if (target.IsWeb)
            {
                var byAccount = target.Connection.Session.Account;
                line.By = byAccount.Name;
                line.ById = byAccount.Id.ToString("D");
                line.ByType = "RohBot";
                line.ByStyle = byAccount.EnabledStyle;
            }
            else
            {
                line.By = target.Persona.DisplayName;
                line.ById = target.Persona.Id.ConvertToUInt64().ToString("D");
                line.ByType = "Steam";
                line.ByStyle = "";
            }

            line.Content = $"{line.For} was banned by {line.By}.";

            target.Room.SendLine(line);

            target.Room.Ban(parameters[0]);
        }
    }
}
