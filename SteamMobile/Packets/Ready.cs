using System;

namespace SteamMobile.Packets
{
    // S -> C
    public class Ready : Packet
    {
        public override string Type { get { return "ready"; } }

        public string SteamId;

        public override void Handle(Session session)
        {
            throw new NotSupportedException();
        }
    }
}
