
namespace RohBot.Commands
{
    public class Default : Command
    {
        public override string Type => "";

        public override string Format(CommandTarget target, string type) => "";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (target.IsWeb || target.IsPrivateChat)
                target.Send("Unknown command.");
        }
    }
}
