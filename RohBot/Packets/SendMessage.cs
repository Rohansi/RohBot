using System;

namespace RohBot.Packets
{
    // C -> S
    public class SendMessage : Packet
    {
        public override string Type => "sendMessage";

        public string Target;
        public string Content;

        public override void Handle(Connection connection)
        {
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
            if (Content.Length > 2000)
                Content = Content.Substring(0, 2000) + "...";

            if (Program.DelayManager.AddAndCheck(connection, CalculateMessageCost(Content)))
                return;

            var room = Program.RoomManager.Get(Target);
            var roomName = room != null ? Target : Program.Settings.DefaultRoom;

            var isEscapedCommand = Content.StartsWith("//");
            if (!isEscapedCommand && Command.Handle(new CommandTarget(connection, roomName), Content, "/"))
                return;

            if (isEscapedCommand)
                Content = Content.Substring(1);

            if (room == null)
            {
                connection.SendSysMessage("RohBot is not in this room.");
                return;
            }

            room.SendMessage(connection, Content);
        }

        private double CalculateMessageCost(string message)
        {
            var newlines = Util.CountNewlines(message);
            var cost = Math.Round(((message.Length / 80f) + newlines), 2); //~80 characters is 1 "line"
            if (cost > 2.5f) //if there's more than 2.5 lines add the amount of lines to the cost
                return cost + 2.5f;
            else
                return 5.0f; 
        }
    }
}
