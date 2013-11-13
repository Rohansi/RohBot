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
                    if (Program.DelayManager.AddAndCheck(session, 10))
                        break;
                    Program.Logger.InfoFormat("Login '{1}' from {0}, password={2}", session.Address, Username, passwordHash);
                    session.Login(Username, Password, (Tokens ?? "").Split(',').ToList());
                    break;
                case "register":
                    if (Program.DelayManager.AddAndCheck(session, 50))
                        break;
                    Program.Logger.InfoFormat("Register '{1}' from {0}, password={2}", session.Address, Username, passwordHash);
                    session.Register(Username, Password);
                    break;
            }
        }
    }
}
