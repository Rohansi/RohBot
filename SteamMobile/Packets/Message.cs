using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Packets
{
    public class Message : Packet
    {
        public override string Type { get { return "message"; } }

        public long Date;
        public string Sender;
        public string Content;
    }
}
