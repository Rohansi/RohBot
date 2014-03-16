namespace SteamMobile.Commands
{
    public class Leave : Command
    {
        public override string Type { get { return "leave"; } }

        public override string Format(CommandTarget target, string type) { return "]"; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || parameters.Length == 0)
                return;

            if (!target.Connection.Session.Leave(parameters[0]))
                target.Send("Room does not exist.");
        }
    }
}
