
namespace RohBot.Packets
{
    public class Ping : Packet
    {
        public override string Type => "ping";

        public override void Handle(Connection connection)
        {
            // do nothing
        }
    }
}
