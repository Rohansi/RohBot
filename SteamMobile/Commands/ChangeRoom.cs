using System.Linq;

namespace SteamMobile.Commands
{
    public class ChangeRoom : Command
    {
        public override string Type { get { return "room"; } }

        public override string Format(CommandTarget target, string type) { return "--"; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsSession || target.Session.Account == null)
                return;

            if (parameters.Length < 1)
            {
                target.Send("Currently in room: " + target.Session.Room);
                return;
            }

            if (Program.DelayManager.AddAndCheck(target.Session, 2.5))
                return;

            switch (parameters[0])
            {
                case "default":
                    {
                        var defaultRoom = target.Session.Account.DefaultRoom;
                        var newRoom = parameters.Length < 2 ? defaultRoom : parameters[1];

                        if (!target.Session.SwitchRoom(newRoom))
                            break;

                        if (parameters.Length >= 2)
                        {
                            target.Session.Account.DefaultRoom = target.Session.Room;
                            target.Session.Account.Save();
                        }

                        target.Send("Switched to room: " + newRoom);
                        break;
                    }

                case "list":
                    {
                        var roomNames = Program.RoomManager.List
                            .Where(r => !r.IsHidden)
                            .Select(r => r.RoomInfo)
                            .Select(r => string.Format("{0} ({1})", r.Name, r.ShortName));
                        target.Send("Available rooms: " + string.Join(", ", roomNames));
                        break;
                    }

                default:
                    {
                        if (!target.Session.SwitchRoom(parameters[0]))
                            break;

                        target.Send("Switched to room: " + parameters[0]);
                        break;
                    }
            }
        }
    }
}
