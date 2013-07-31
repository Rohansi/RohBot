using System;
using System.Collections.Generic;

namespace SteamMobile.Packets
{
    public class ChatHistory : Packet
    {
        public override string Type { get { return "chatHistory"; } }

        public bool Requested;
        public string Chat;
        public long OldestLine
        {
            get { return Lines != null && Lines.Count > 0 ? Lines[0].Date : long.MaxValue; }
            set { /* do nothing */ }
        }

        public List<HistoryLine> Lines;
    }
}
