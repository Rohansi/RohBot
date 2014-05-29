
namespace RohBot.Commands
{
    public class Kick : Command
    {
        public override string Type { get { return "kick"; } }

        public override string Format(CommandTarget target, string type) { return "]"; }

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

            target.Send(string.Format("Kicked session ({0} connections).", connectionCount));
        }
    }
}
