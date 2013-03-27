using System;

namespace SteamMobile.Packets
{
    public class ClientPermissions : Packet
    {
        public override string Type { get { return "clientPermissions"; } }

        public string Username;
        public bool CanChat;
    }
}
