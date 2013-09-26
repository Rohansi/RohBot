using System;

namespace SteamMobile.Packets
{
    public class AuthenticateResponse : Packet
    {
        public override string Type
        {
            get { return "authResponse"; }
        }

        public bool Success;
        public string Name;
        public string Tokens;

        public override void Handle(Session session)
        {
            throw new NotSupportedException();
        }
    }
}
