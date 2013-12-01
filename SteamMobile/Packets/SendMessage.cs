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
                session.SendSysMessage("Guests can not speak.");
                return;
            }

            Content = Content.Trim();

            if (Content.Length == 0)
                return;

            // can't send emoticons from web
            Content = Content.Replace('ː', ':');

            // steam discards long messages
            if (Content.Length > 2000)
                Content = Content.Substring(0, 2000) + "...";

            Room room = Program.RoomManager.Get(session.Room);
            if (room == null)
            {
                if (Command.Handle(new CommandTarget(session), Content, "/"))
                    return;

                if (Command.Handle(new CommandTarget(session), Content, "~"))
                    return;

                session.SendSysMessage("RohBot is not in this room.");
                return;
            }

            if (Program.DelayManager.AddAndCheck(session, 1))
                return;

            room.SendMessage(session, Content);
        }
    }
}
