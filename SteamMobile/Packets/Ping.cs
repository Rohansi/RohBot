using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Packets
{
    public class Ping : Packet
    {
        public override string Type { get { return "ping"; } }
    }
}
