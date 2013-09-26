
namespace SteamMobile.Commands
{
    public class ChangeRoom : Command
    {
        public override string Type { get { return "room"; } }

        public override string Format { get { return "--"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession || target.Session.Account == null)
                return;

            if (parameters.Length < 1)
            {
                target.Send("Currently viewing: " + target.Session.Room);
                return;
            }

            switch (parameters[0])
            {
                case "default":
                    {
                        var defaultRoom = target.Session.Account.DefaultRoom;
                        var set = (parameters.Length < 2 ? defaultRoom : parameters[1]).ToLower();

                        var room = Program.RoomManager.Get(set);
                        if (room == null)
                        {
                            target.Send("Room does not exist.");
                            return;
                        }

                        target.Session.Room = set;

                        if (parameters.Length >= 2)
                        {
                            target.Session.Account.DefaultRoom = set;
                            Database.Accounts.Save(target.Session.Account);
                        }

                        room.SendHistory(target.Session);
                        target.Send("Switched to room: " + set);
                        break;
                    }

                case "list":
                    {
                        target.Send("Available rooms: " + string.Join(", ", Program.RoomManager.Names));
                        break;
                    }

                default:
                    {
                        var set = parameters[0].ToLower();
                        var room = Program.RoomManager.Get(set);
                        if (room == null)
                        {
                            target.Send("Chat does not exist.");
                            return;
                        }

                        target.Session.Room = set;
                        room.SendHistory(target.Session);
                        target.Send("Switched to chat: " + set);
                        break;
                    }
            }
        }
    }
}
