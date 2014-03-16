
namespace SteamMobile.Commands
{
    public class Default : Command
    {
        public override string Type { get { return ""; } }

        public override string Format(CommandTarget target, string type) { return ""; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (target.IsWeb || target.IsPrivateChat)
                target.Send("Unknown command.");
        }
    }
}
