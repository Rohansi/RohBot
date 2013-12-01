using System;
using System.Collections.Generic;
using System.Linq;

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
            if (Program.DelayManager.AddAndCheck(session, 2.5))
                return;

            var room = Program.RoomManager.Get(session.Room);
            if (room == null)
            {
                session.SendSysMessage("Room does not exist.");
                return;
            }

            List<HistoryLine> lines;

            if (Util.DateTimeFromUnixTimestamp(AfterDate) > DateTime.UtcNow.AddDays(-7))
            {
                var cmd = new SqlCommand("SELECT * FROM rohbot.chathistory WHERE chat=lower(:chat) AND date<:afterdate ORDER BY date DESC LIMIT 100;");
                cmd["chat"] = session.Room;
                cmd["afterdate"] = AfterDate;
                lines = cmd.Execute().Select(r => (HistoryLine)HistoryLine.Read(r)).Reverse().ToList();
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
