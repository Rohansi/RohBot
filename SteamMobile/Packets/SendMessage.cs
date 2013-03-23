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

            if (Program.MainChat == null || (packet.Content).Length == 0)
                return;

            var m = packet.Content;

            // fpp filters
            m = m.Replace("kick_me", "****");
            if (m.Contains("http") && m.Contains("window.location.href"))
                m = m.Replace("http", "****");

            if (m.StartsWith("/list"))
            {
                var list = Program.MainChat.Members.Select(id => Steam.Friends.GetFriendPersonaName(id)).OrderBy(n => n);
                Program.SendMessage(session.Socket, "*", "In this chat: " + string.Join(", ", list));
                return;
            }

            if (m.StartsWith("/status"))
            {
                var str = new StringBuilder();
                str.AppendFormat("Steam Status: {0}\n", Steam.Status);
                str.AppendFormat("Active SteamChats: {0}", string.Join(", ", Steam.Chats.Select(c => string.Format("{0} ({1}, {2})", c.Title, c.RoomId, c.Response))));
                Program.SendMessage(session.Socket, "*", str.ToString());
                return;
            }

            if (m.StartsWith("/me "))
            {
                var action = m.Substring(4);
                m = session.Name + " " + action;
                Program.MainChat.Send(m);
                Program.HandleMessage(Program.MainChat, Steam.Client.SteamID, m);
                return;
            }

            /*if (m.StartsWith("/sessions"))
            {
                var sess = sessions.Values.Select(ss => ss.Name).ToList();
                var req = sess.Distinct().Select(na => na + (sess.Count(sss => )));
                return;
            }*/

            var msg = string.Format("[{0}] {1}", session.Name, m);
            Program.MainChat.Send(msg);
            Program.HandleMessage(Program.MainChat, Steam.Client.SteamID, msg);
        }
    }
}
