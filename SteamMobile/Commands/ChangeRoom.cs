using System.Linq;

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

            if (Program.DelayManager.AddAndCheck(target.Session, 5))
                return;

            switch (parameters[0])
            {
                case "default":
                    {
                        var defaultRoom = target.Session.Account.DefaultRoom;
                        var set = parameters.Length < 2 ? defaultRoom : parameters[1];

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
                        var roomNames = Program.RoomManager.List.Where(r => !r.IsHidden).Select(r => r.RoomInfo.ShortName);
                        target.Send("Available rooms: " + string.Join(", ", roomNames));
                        break;
                    }

                default:
                    {
                        var room = Program.RoomManager.Get(parameters[0]);
                        if (room == null)
                        {
                            target.Send("Room does not exist.");
                            return;
                        }

                        target.Session.Room = parameters[0];
                        room.SendHistory(target.Session);
                        target.Send("Switched to room: " + parameters[0]);
                        break;
                    }
            }
        }
    }
}
