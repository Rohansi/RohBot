
namespace RohBot.Commands
{
    public class Join : Command
    {
        public override string Type => "join";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || parameters.Length == 0)
                return;

            if (Program.DelayManager.AddAndCheck(target.Connection, DelayManager.Database))
                return;

            if (!target.Connection.Session.Join(parameters[0]))
                target.Send("Room does not exist.");
        }
    }
}
