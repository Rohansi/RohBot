
namespace SteamMobile.Packets
{
    public class Ping : Packet
    {
        public override string Type { get { return "ping"; } }

        public override void Handle(Connection connection)
        {
            // do nothing
        }
    }
}
