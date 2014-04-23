
namespace SteamMobile.Commands
{
    public class Unban : Command
    {
        public override string Type { get { return "unban"; } }

        public override string Format(CommandTarget target, string type) { return "]"; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !Util.IsMod(target) || parameters.Length == 0)
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
                target.Send("Account is not banned.");
                return;
            }

            target.Room.Unban(parameters[0]);

            var line = new StateLine
            {
                Date = Util.GetCurrentTimestamp(),
                Chat = target.Room.RoomInfo.ShortName,
                State = "Unbanned",
                For = forAccount.Name,
                ForId = forAccount.Id.ToString("D"),
                ForType = "RohBot"
            };

            if (target.IsWeb)
            {
                var byAccount = target.Connection.Session.Account;
                line.By = byAccount.Name;
                line.ById = byAccount.Id.ToString("D");
                line.ByType = "RohBot";
            }
            else
            {
                line.By = target.Persona.DisplayName;
                line.ById = target.Persona.Id.ConvertToUInt64().ToString("D");
                line.ByType = "Steam";
            }

            line.Content = string.Format("{0} was unbanned by {1}.", line.For, line.By);

            target.Room.SendLine(line);
        }
    }
}
