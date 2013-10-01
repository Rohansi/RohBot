using System;
using System.Collections.Generic;
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

        public override void Handle(Session session)
        {
            var room = Program.RoomManager.Get(session.Room);
            if (room == null)
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "Room does not exist."
                });
                return;
            }

            List<HistoryLine> lines;

            if (Util.DateTimeFromUnixTimestamp(AfterDate) > DateTime.UtcNow.AddDays(-7))
            {
                lines = Database.ChatHistory.AsQueryable()
                                .Where(r => r.Chat == session.Room && r.Date < AfterDate)
                                .OrderByDescending(r => r.Date)
                                .Take(100).ToList();
                lines.Reverse();
            }
            else
            {
                lines = new List<HistoryLine>();
            }
            
            var history = new ChatHistory
            {
                Name = room.RoomInfo.Name,
                ShortName = room.RoomInfo.ShortName,
                Requested = true,
                Lines = lines
            };

            session.Send(history);
        }
    }
}
