namespace SteamMobile.Commands
{
    public class Join : Command
    {
        public override string Type { get { return "join"; } }

        public override string Format(CommandTarget target, string type) { return "]"; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || parameters.Length == 0)
                return;

            if (Program.DelayManager.AddAndCheck(target.Connection, 10))
                return;

            if (!target.Connection.Session.Join(parameters[0]))
                target.Send("Room does not exist.");
        }
    }
}
