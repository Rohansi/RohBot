using System.Linq;
using SteamMobile.Rooms;

namespace SteamMobile.Commands
{
    public class FixSteam : Command
    {
        public override string Type { get { return "fixsteam"; } }

        public override string Format(string type) { return ""; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target))
                return;

            foreach (var room in Program.RoomManager.List.OfType<SteamRoom>())
            {
                room.Leave(); // RoomManager will put us back in
            }
        }
    }
}
