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
        public string Room;

        public override void Handle(Session session)
        {
            switch (Method)
            {
                case "login":
                    if (Program.DelayManager.AddAndCheck(session, 10))
                        break;
                    Program.Logger.InfoFormat("Login '{1}' from {0}", session.Address, Username);
                    session.Login(Username, Password, (Tokens ?? "").Split(',').ToList(), Room);
                    break;
                case "register":
                    if (Program.DelayManager.AddAndCheck(session, 10))
                        break;
                    Program.Logger.InfoFormat("Register '{1}' from {0}", session.Address, Username);
                    session.Register(Username, Password);
                    break;
            }
        }
    }
}
