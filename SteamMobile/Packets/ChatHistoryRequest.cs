using System;
using System.Linq;
using MongoDB.Driver.Linq;

namespace SteamMobile.Packets
{
    public class ChatHistoryRequest : Packet
    {
        public override string Type
        {
            get { return "chatHistoryRequest"; }
        }

        public long AfterDate;

        public static void Handle(Session session, Packet pack)
        {
            var packet = (ChatHistoryRequest)pack;

            var lines = Database.ChatHistory.AsQueryable()
                                .Where(r => r.Chat == session.Chat && r.Date < packet.AfterDate)
                                .OrderByDescending(r => r.Date)
                                .Take(100).ToList();
            lines.Reverse();
            
            var historyPack = new ChatHistory()
            {
                Requested = true,
                Chat = session.Chat,
                Lines = lines
            };

            Program.Send(session, historyPack);
        }
    }
}
