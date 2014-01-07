using System.Linq;
using SteamMobile.Rooms;

namespace SteamMobile.Commands
{
    public class FixSteam : Command
    {
        public override string Type { get { return "fixsteam"; } }

        public override string Format(CommandTarget target, string type) { return "-"; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target))
                return;

            Program.Steam.Disconnect(); // should auto-reconnect
        }
    }
}
