using System.Linq;

namespace SteamMobile.Packets
{
    public class Authenticate : Packet
    {
        public override string Type
        {
            get { return "auth"; }
        }

        public string Method; // login/register
        public string Username;
        public string Password;
        public string Tokens;

        public override void Handle(Session session)
        {
            var passwordHash = Password != null ? Password.GetHashCode() : 0;

            switch (Method)
            {
                case "login":
                    Program.Logger.InfoFormat("Login '{1}' from {0}, password={2}", session.Socket.ConnectionInfo.ClientIpAddress, Username, passwordHash);
                    session.Login(Username, Password, (Tokens ?? "").Split(',').ToList());
                    break;
                case "register":
                    Program.Logger.InfoFormat("Register '{1}' from {0}, password={2}", session.Socket.ConnectionInfo.ClientIpAddress, Username, passwordHash);
                    session.Register(Username, Password);
                    break;
            }
        }
    }
}
