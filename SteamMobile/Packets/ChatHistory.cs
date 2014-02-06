using System;
using System.Collections.Generic;

namespace SteamMobile.Packets
{
    // S -> C
    public class ChatHistory : Packet
    {
        public override string Type { get { return "chatHistory"; } }

        public string Name;
        public string ShortName;
        public bool Requested;
        public List<HistoryLine> Lines;
        public long OldestLine
        {
            get { return Lines != null && Lines.Count > 0 ? Lines[0].Date : 0; }
            set { /* do nothing */ }
        }

        public override void Handle(Session session)
        {
            throw new NotSupportedException();
        }
    }
}
