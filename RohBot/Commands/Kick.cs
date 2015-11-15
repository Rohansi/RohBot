
namespace RohBot.Commands
{
    public class Kick : Command
    {
        public override string Type => "kick";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target) || parameters.Length == 0)
                return;

            if (!Util.IsValidUsername(parameters[0]))
            {
                target.Send("Invalid username.");
                return;
            }

            var session = Program.SessionManager.Get(parameters[0]);
            if (session == null)
            {
                target.Send("Session does not exist.");
                return;
            }

            var connectionCount = session.ConnectionCount;
            session.Destroy();

            target.Send($"Kicked session ({connectionCount} connections).");
        }
    }
}
