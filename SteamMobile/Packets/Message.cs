using System;

namespace SteamMobile.Packets
{
    public class Message : Packet
    {
        public override string Type { get { return "message"; } }

        public HistoryLine Line;
    }
}
