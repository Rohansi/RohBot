
namespace SteamMobile.Commands
{
    public class Ban : Command
    {
        public override string Type { get { return "ban"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
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
            
            if (!target.Room.IsWhitelisted)
                target.Room.Ban(parameters[0]);
            else
                target.Room.Unban(parameters[0]);

            target.Send("Account banned.");
        }
    }
}
