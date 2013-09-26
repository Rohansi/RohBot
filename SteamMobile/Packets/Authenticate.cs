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
            switch (Method)
            {
                case "login":
                    session.Login(Username, Password, (Tokens ?? "").Split(',').ToList());
                    break;
                case "register":
                    session.Register(Username, Password);
                    break;
            }
        }
    }
}
