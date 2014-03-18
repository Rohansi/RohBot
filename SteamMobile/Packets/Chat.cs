using System;

namespace SteamMobile.Packets
{
    public class Chat : Packet
    {
        public override string Type { get { return "chat"; } }

        public string Method; // join/leave
        public string Name;
        public string ShortName;

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
