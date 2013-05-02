using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            if (Program.MainChat == null || message.Length == 0)
                return;

            // fpp filters
            message = message.Replace("kick_me", "****");
            if (message.Contains("http") && message.Contains("window.location.href"))
                message = message.Replace("http", "****");

            if (!message.StartsWith("//") && Command.Handle(CommandTarget.FromSession(session), message))
                return;

            if (message.StartsWith("//"))
                message = message.Substring(1);

            if (!session.Permissions.HasFlag(Permissions.Chat))
                return;

            message = string.Format("[{0}] {1}", session.Name, message);
            Program.MainChat.Send(message);
        }
    }
}
