
namespace SteamMobile.Commands
{
    public class Reload : Command
    {
        public override string Type { get { return "reload"; } }

        // up to 10 parameters
        public override string Format { get { return "----------"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target))
                return;

            Program.LoadSettings();

            foreach (var roomName in parameters)
            {
                Program.RoomManager.Get(roomName).Leave();
            }

            target.Send("Done.");
        }
    }
}
