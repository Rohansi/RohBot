using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Packets
{
    public class ChatHistory : Packet
    {
        public override string Type { get { return "chatHistory"; } }

        public IEnumerable<ChatLine> Lines;
    }
}
