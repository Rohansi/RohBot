using RohBot.Rooms.Steam;

namespace RohBot.Commands
{
    public class Status : Command
    {
        public override string Type => "status";

        public override string Format(CommandTarget target, string type) => "";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            var steamOnline = Program.Steam.Status == Steam.ConnectionStatus.Connected;
            target.Send("Steam: " + (steamOnline ? "Online" : "Offline"));
        }
    }
}
