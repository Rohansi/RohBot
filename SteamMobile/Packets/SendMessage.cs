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

            if (!session.Permissions.HasFlag(Permissions.Chat))
                return;

            if (Program.MainChat == null || packet.Content.Length == 0) // TODO: warn user if mainchat == null
                return;

            var message = packet.Content.Trim();

            // fpp filters
            message = message.Replace("kick_me", "****");
            if (message.Contains("http") && message.Contains("window.location.href"))
                message = message.Replace("http", "****");

            if (Command.Handle(CommandTarget.FromSession(session), message))
                return;

            message = string.Format("[{0}] {1}", session.Name, message);
            Program.MainChat.Send(message);
        }
    }
}
