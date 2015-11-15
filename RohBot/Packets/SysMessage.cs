using System;

namespace RohBot.Packets
{
    // S -> C
    public class SysMessage : Packet
    {
        public override string Type => "sysMessage";

        public long Date;
        public string Content;

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
