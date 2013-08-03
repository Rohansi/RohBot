using System;
using System.Linq;

namespace SteamMobile.Commands
{
    public class Sessions : Command
    {
        public override string Type { get { return "sessions"; } }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            var sessions = Program.Sessions.Values.Select(ss => ss.Name).ToList();
            var req = sessions.Distinct().Select(name =>
            {
                var count = sessions.Count(s => s == name);
                return name + (count > 1 ? string.Format(" ({0})", count) : "");
            });

            var msg = "Active sessions: " + string.Join(", ", req);
            target.Send(msg);
        }
    }
}
