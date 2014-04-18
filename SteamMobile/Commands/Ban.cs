
namespace SteamMobile.Commands
{
    public class Ban : Command
    {
        public override string Type { get { return "ban"; } }

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

            if (!Account.Exists(parameters[0]))
            {
                target.Send("Account does not exist.");
                return;
            }

            if (Util.IsAdmin(target.Room, parameters[0]))
            {
                target.Send("Administrators can not be banned.");
                return;
            }

            string sourceUser;
            if (target.IsWeb)
                sourceUser = target.Connection.Session.Account.Name;
            else
                sourceUser = string.Format("steam:{0:D}", (ulong)target.SteamId);

            Program.Logger.InfoFormat("User '{0}' banning '{1}'", sourceUser, parameters[0]);
            
            target.Room.Ban(parameters[0]);
            target.Send("Account banned.");
        }
    }
}
