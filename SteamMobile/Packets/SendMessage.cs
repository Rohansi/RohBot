namespace SteamMobile.Packets
{
    // C -> S
    public class SendMessage : Packet
    {
        public override string Type { get { return "sendMessage"; } }

        public string Target;
        public string Content;

        public override void Handle(Connection connection)
        {
            if (Program.DelayManager.AddAndCheck(connection, 1))
                return;

            if (connection.Session == null)
            {
                connection.SendSysMessage("Guests can not speak.");
                return;
            }

            Content = (Content ?? "").Trim();

            if (Content.Length == 0)
                return;

            // can't send emoticons from web
            Content = Content.Replace('ː', ':');

            // steam discards long messages
            if (Content.Length > 1000)
                Content = Content.Substring(0, 1000) + "...";

            var room = Program.RoomManager.Get(Target);
            if (room == null)
            {
                if (Command.Handle(new CommandTarget(connection, Program.Settings.DefaultRoom), Content, "/"))
                    return;

                if (Command.Handle(new CommandTarget(connection, Program.Settings.DefaultRoom), Content, "~"))
                    return;

                connection.SendSysMessage("RohBot is not in this room.");
                return;
            }

            room.SendMessage(connection, Content);
        }
    }
}
