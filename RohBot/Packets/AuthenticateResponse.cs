using System;

namespace RohBot.Packets
{
    public class AuthenticateResponse : Packet
    {
        public override string Type => "authResponse";

        public string Name;
        public string Tokens;
        public bool Success;

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
