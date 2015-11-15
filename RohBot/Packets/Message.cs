using System;

namespace RohBot.Packets
{
    // S -> C
    public class Message : Packet
    {
        public override string Type => "message";

        public HistoryLine Line;

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
