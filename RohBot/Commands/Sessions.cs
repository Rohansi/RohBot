using System.Linq;

namespace RohBot.Commands
{
    public class Sessions : Command
    {
        public override string Type => "sessions";

        public override string Format(CommandTarget target, string type) => "";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || !Util.IsSuperAdmin(target))
                return;

            var sessions = Program.SessionManager.List.Select(s => $"{s.Account.Name} ({s.ConnectionCount})");
            var sessionsText = string.Join(", ", sessions);
            target.Send($"Sessions: {sessionsText}");
        }
    }
}
