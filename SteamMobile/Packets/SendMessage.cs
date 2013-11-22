using SteamMobile.Rooms;

namespace SteamMobile.Packets
{
    // C -> S
    public class SendMessage : Packet
    {
        public override string Type { get { return "sendMessage"; } }

        public string Content = null;

        public override void Handle(Session session)
        {
            if (session.Account == null)
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "Guests can not speak."
                });
                return;
            }

            Content = Content.Trim();

            if (Content.Length == 0)
                return;

            if (!Content.StartsWith("//") && Command.Handle(new CommandTarget(session), Content))
                return;

            if (!Content.StartsWith("~~") && Command.Handle(new CommandTarget(session), Content, "~"))
                return;

            Room room = Program.RoomManager.Get(session.Room);
            if (room == null)
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "RohBot is not in this room."
                });
                return;
            }

            if (room.IsBanned(session.Account.Name))
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "You are banned from this room."
                });
                return;
            }

            if (Program.DelayManager.AddAndCheck(session, 0.1))
                return;

            if (Content.StartsWith("//") || Content.StartsWith("~~"))
                Content = Content.Substring(1);

            // can't send emoticons from web
            Content = Content.Replace('ː', ':');

            // steam discards long messages
            if (Content.Length > 2000)
                Content = Content.Substring(0, 2000) + "...";

            var roomName = room.RoomInfo.ShortName;
            var userName = session.Account.Name;
            var userId = session.Account.Id.ToString();
            var userStyle = session.Account.EnabledStyle;
            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), roomName, "RohBot", userName, userId, userStyle, Content, false);
            room.SendLine(line);
        }
    }
}
