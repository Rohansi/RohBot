using System;

namespace SteamMobile.Packets
{
    public class SysMessage : Packet
    {
        public override string Type { get { return "sysMessage"; } }

        public long Date;
        public string Content;
    }
}
