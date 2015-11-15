
namespace RohBot.Commands
{
    public class Broadcast : Command
    {
        public override string Type => "broadcast";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target) || parameters.Length < 1)
                return;

            Program.RoomManager.Broadcast("Broadcast: " + parameters[0]);
        }
    }
}
