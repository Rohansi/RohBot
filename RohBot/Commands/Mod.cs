
namespace RohBot.Commands
{
    public class Mod : Command
    {
        public override string Type => "mod";

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

            if (target.Room.IsMod(parameters[0]))
            {
                target.Send("Account is already a mod.");
                return;
            }

            target.Room.Mod(parameters[0]);
            target.Send("Account modded.");
        }
    }
}
