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

            if (room.IsPrivate)
            {
                if (session.Account == null || room.IsBanned(session.Account.Name))
                    return;
            }

            var cmd = new SqlCommand("SELECT * FROM rohbot.chathistory WHERE chat=lower(:chat) AND date<:afterdate ORDER BY date DESC LIMIT 100;");
            cmd["chat"] = session.Room;
            cmd["afterdate"] = AfterDate;
            var lines = cmd.Execute().Select(r => (HistoryLine)HistoryLine.Read(r)).Reverse().ToList();

            if (lines.Count == 0)
                lines.Add(new ChatLine(0, session.Room, "Steam", "~", "0", "", "No additional history is available.", false));
            
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
