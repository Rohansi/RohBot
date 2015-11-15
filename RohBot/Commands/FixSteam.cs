
namespace RohBot.Commands
{
    public class FixSteam : Command
    {
        public override string Type => "fixsteam";

        public override string Format(CommandTarget target, string type) => "";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!Util.IsSuperAdmin(target))
                return;

            Program.Steam.Disconnect(); // should auto-reconnect
        }
    }
}
