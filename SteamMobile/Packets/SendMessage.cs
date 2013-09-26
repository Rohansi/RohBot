using System;

namespace SteamMobile.Packets
{
    // C -> S
    public class SendMessage : Packet
    {
        public override string Type { get { return "sendMessage"; } }

        public string Content = null;

        public override void Handle(Session session)
        {
            if (session.AccountInfo.SteamId == "0")
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "Guests can not speak."
                });
                return;
            }

            Room room = Program.RoomManager.Get(session.Room);
            if (room == null)
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "RohBot is not in the current chat."
                });
                return;
            }

            if (room.IsBanned(ulong.Parse(session.AccountInfo.SteamId)))
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "You are banned from this chat."
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

            if (Content.StartsWith("//") || Content.StartsWith("~~"))
                Content = Content.Substring(1);

            if (session.AccountInfo.Name == null)
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "You have not set your name yet. To set your name, type: /name YourNameHere"
                });
                return;
            }

            // can't send emoticons from web
            Content = Content.Replace('ː', ':');

            // steam discards long messages
            if (Content.Length > 2000)
                Content = Content.Substring(0, 2000) + "...";

            var roomName = room.RoomInfo.ShortName;
            var userName = session.AccountInfo.Name;
            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), roomName, "RohBot", userName, session.AccountInfo.SteamId, Content, false);
            room.Send(line);
        }
    }
}
