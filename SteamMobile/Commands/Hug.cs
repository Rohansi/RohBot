namespace SteamMobile.Commands
{
    public class Hug : Command
    {
        public override string Type { get { return "hug"; } }

        public override string Format(CommandTarget target, string type) { return "]"; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || parameters.Length == 0)
                return;

            if (Program.DelayManager.AddAndCheck(target.Connection, 1))
                return;

            var username = target.Connection.Session.Account.Name;
            var room = target.Room;
            if (room.IsBanned(username))
            {
                target.Send("You are banned from this room.");
                return;
            }

            room.Send(string.Format("{0} hugs {1}.", username, parameters[0]));
        }
    }
}
