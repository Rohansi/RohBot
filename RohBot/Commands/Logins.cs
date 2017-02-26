using System.Linq;
using System.Text;

namespace RohBot.Commands
{
    public class Logins : Command
    {
        private static readonly UAParser.Parser UserAgentParser = UAParser.Parser.GetDefault(); 

        public override string Type => "logins";

        public override string Format(CommandTarget target, string type) => "-";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || Program.DelayManager.AddAndCheck(target.Connection, DelayManager.Database))
                return;

            var userId = target.Connection.Session.Account.Id;

            if (parameters.Length == 1 && parameters[0] == "clear")
            {
                LoginToken.RemoveAll(userId);
                target.Send("Cleared all logins.");
                return;
            }
            
            var loginTokens = LoginToken.FindAll(userId).ToList();

            if (loginTokens.Count == 0)
            {
                target.Send("No active logins.");
                return;
            }

            var sb = new StringBuilder();

            sb.AppendLine("Active logins:");
            
            foreach (var login in loginTokens)
            {
                var address = login.Address;
                var ua = UserAgentParser.Parse(login.UserAgent);
                var created = Util.DateTimeFromTimestamp(login.Created);
                var accessed = Util.DateTimeFromTimestamp(login.Accessed);

                sb.AppendFormat("{0} - {1} (created {2}, last accessed {3})", address, ua, created, accessed);
                sb.AppendLine();
            }

            target.Send(sb.ToString().Trim());
        }
    }
}
