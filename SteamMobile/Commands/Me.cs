using SteamMobile.Packets;

namespace SteamMobile.Commands
{
    public class Me : Command
    {
        public override string Type { get { return "me"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession || target.Session.Account == null || parameters.Length == 0)
                return;

            if (Program.DelayManager.AddAndCheck(target.Session, 1))
                return;

            var room = Program.RoomManager.Get(target.Session.Room);
            if (room == null)
            {
                target.Session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "RohBot is not in this room."
                });
                return;
            }

            if (room.IsBanned(target.Session.Account.Name))
            {
                target.Send("You are banned from this room.");
                return;
            }

            room.Send(string.Format("{0} {1}", target.Session.Account.Name, parameters[0]));
        }
    }
}
