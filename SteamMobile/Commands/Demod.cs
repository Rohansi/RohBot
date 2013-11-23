
namespace SteamMobile.Commands
{
    public class Demod : Command
    {
        public override string Type { get { return "demod"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsRoom || !Util.IsAdmin(target) || parameters.Length == 0)
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

            target.Room.Demod(parameters[0]);
            target.Send("Account demodded.");
        }
    }
}
