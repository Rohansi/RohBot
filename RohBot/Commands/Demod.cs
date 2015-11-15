
namespace RohBot.Commands
{
    public class Demod : Command
    {
        public override string Type => "demod";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
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

            if (!target.Room.IsMod(parameters[0]))
            {
                target.Send("Account is not a mod.");
                return;
            }

            target.Room.Demod(parameters[0]);
            target.Send("Account demodded.");
        }
    }
}
