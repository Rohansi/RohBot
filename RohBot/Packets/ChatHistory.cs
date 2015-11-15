using System;
using System.Collections.Generic;
using System.Linq;

namespace RohBot.Packets
{
    // S -> C
    public class ChatHistory : Packet
    {
        public override string Type => "chatHistory";

        public string ShortName;
        public bool Requested;
        public ICollection<HistoryLine> Lines;
        public long OldestLine
        {
            get { return Lines != null && Lines.Count > 0 ? Lines.First().Date : 0; }
            set { /* do nothing */ }
        }

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
