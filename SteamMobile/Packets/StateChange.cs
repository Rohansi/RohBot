using System;

namespace SteamMobile.Packets
{
    public class StateChange : Packet
    {
        public override string Type { get { return "stateChange"; } }

        public long Date;
        public string Content;
    }
}
