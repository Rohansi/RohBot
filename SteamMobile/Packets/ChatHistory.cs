using System;
using System.Collections.Generic;

namespace SteamMobile.Packets
{
    public class ChatHistory : Packet
    {
        public override string Type { get { return "chatHistory"; } }

        public IEnumerable<HistoryLine> Lines;
    }
}
