using System;
using System.Collections.Generic;
using System.Linq;

namespace RohBot.Packets
{
    public class ChatHistoryRequest : Packet
    {
        public override string Type => "chatHistoryRequest";

        public string Target;
        public long AfterDate;

        public override void Handle(Connection connection)
        {
            if (Program.DelayManager.AddAndCheck(connection, DelayManager.Database))
                return;

            if (connection.Session == null)
            {
                connection.SendSysMessage("You need to be logged in to do that.");
                return;
            }

            if (!connection.Session.IsInRoom(Target))
            {
                connection.SendSysMessage("You are not in that room.");
                return;
            }

            var room = Program.RoomManager.Get(Target);
            if (room == null)
            {
                connection.SendSysMessage("Room does not exist.");
                return;
            }

            if (room.IsPrivate && room.IsBanned(connection.Session.Account.Name))
                return;
            
            List<HistoryLine> lines;
            if (Util.DateTimeFromTimestamp(AfterDate) < (DateTime.UtcNow - Util.MaximumHistoryRequest))
            {
                lines = new List<HistoryLine>();
            }
            else
            {
                var cmd = new SqlCommand("SELECT * FROM rohbot.chathistory WHERE chat=lower(:chat) AND date<:afterdate ORDER BY date DESC LIMIT 100;");
                cmd["chat"] = Target;
                cmd["afterdate"] = AfterDate;

                lines = cmd.Execute().Select(r => (HistoryLine)HistoryLine.Read(r)).ToList();
                lines.Reverse();
            }

            if (lines.Count == 0)
                lines.Add(new ChatLine(0, Target, "Steam", Program.Settings.PersonaName, "0", "", "No additional history is available.", false));
            
            var history = new ChatHistory
            {
                ShortName = room.RoomInfo.ShortName,
                Requested = true,
                Lines = lines
            };

            connection.Send(history);
        }
    }
}
