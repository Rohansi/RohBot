using System;
using System.Text.RegularExpressions;

namespace SteamMobile.Packets
{
    public class SendMessage : Packet
    {
        public override string Type { get { return "sendMessage"; } }

        public string Content = null;

        public static void Handle(Session session, Packet pack)
        {
            var packet = (SendMessage)pack;
            var message = packet.Content.Trim();

            if (message.Length == 0)
                return;

            if (!message.StartsWith("//") && Command.Handle(CommandTarget.FromSession(session), message))
                return;

            if (!message.StartsWith("~~") && Command.Handle(CommandTarget.FromSession(session), message, "~"))
                return;

            if (message.StartsWith("//") || message.StartsWith("~~"))
                message = message.Substring(1);

            if (!session.Permissions.HasFlag(Permissions.Chat))
                return;

            GroupChat chat;
            if (!Program.Chats.TryGetValue(session.Chat, out chat))
            {
                Program.SendSysMessage(session, "RohBot is not in the current chat.");
                return;
            }

            // can't send emoticons from web
            message = message.Replace('ː', ':');

            message = string.Format("[{0}] {1}", session.Name, message);
            chat.Send(message);
        }
    }
}
