using System.Linq;

namespace SteamMobile.Commands
{
    public class Sessions : Command
    {
        public override string Type { get { return "sessions"; } }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            var sessions = Program.SessionManager.List.Where(s => s.Account != null).Select(s => s.Account.Name).ToList();
            var req = sessions.Distinct().Select(n =>
            {
                var count = sessions.Count(s => s == n);
                return n + (count > 1 ? string.Format(" ({0})", count) : "");
            });

            var msg = "Active sessions: " + string.Join(", ", req);
            target.Send(msg);
        }
    }
}
