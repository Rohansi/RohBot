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
            var packet = (Packets.SendMessage)pack;

            if (!session.Permissions.HasFlag(Permissions.Chat))
                return;

            if (Program.MainChat == null || packet.Content.Length == 0) // TODO: warn user if mainchat == null
                return;

            var msg = packet.Content;

            // fpp filters
            msg = msg.Replace("kick_me", "****");
            if (msg.Contains("http") && msg.Contains("window.location.href"))
                msg = msg.Replace("http", "****");

            if (msg.StartsWith("/list"))
            {
                var list = Program.MainChat.Members.Select(id => Steam.Friends.GetFriendPersonaName(id)).OrderBy(n => n);
                Program.SendMessage(session.Socket, "*", "In this chat: " + string.Join(", ", list));
                return;
            }

            if (msg.StartsWith("/status"))
            {
                var str = new StringBuilder();
                str.AppendFormat("Steam Status: {0}\n", Steam.Status);
                str.AppendFormat("Active SteamChats: {0}", string.Join(", ", Steam.Chats.Select(c => string.Format("{0} ({1}, {2})", c.Title, c.RoomId, c.Response))));
                Program.SendMessage(session.Socket, "*", str.ToString());
                return;
            }

            if (msg.StartsWith("/me "))
            {
                var action = msg.Substring(4);
                msg = session.Name + " " + action;
                Program.MainChat.Send(msg);
                Program.HandleMessage(Program.MainChat, Steam.Client.SteamID, msg);
                return;
            }

            if (msg.StartsWith("/sessions"))
            {
                var sessions = Program.Sessions.Values.Select(ss => ss.Name).ToList();
                var req = sessions.Distinct().Select(name =>
                {
                    var count = sessions.Count(s => s == name);
                    return name + (count > 1 ? string.Format(" ({0})", count) : "");
                });
                Program.SendMessage(session.Socket, "*", "Active sessions: " + string.Join(", ", req));
                return;
            }

            var finalMessage = string.Format("[{0}] {1}", session.Name, msg);
            Program.MainChat.Send(finalMessage);
            Program.HandleMessage(Program.MainChat, Steam.Client.SteamID, finalMessage);
        }
    }
}
