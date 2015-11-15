
namespace RohBot.Commands
{
    public class Leave : Command
    {
        public override string Type => "leave";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || parameters.Length == 0)
                return;

            if (Program.DelayManager.AddAndCheck(target.Connection, DelayManager.Database))
                return;

            target.Connection.Session.Leave(parameters[0]);
        }
    }
}
