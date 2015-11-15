using System;

namespace RohBot.Packets
{
    public class Chat : Packet
    {
        public override string Type => "chat";

        public string Method; // join/leave
        public string Name;
        public string ShortName;

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
