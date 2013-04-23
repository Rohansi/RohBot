using System;

namespace SteamMobile.Packets
{
    public class Login : Packet
    {
        public override string Type { get { return "login"; } }

        public string Username = null;
        public string Password = null;

        // TODO: clean and better responses
        public static void Handle(Session session, Packet pack)
        {
            var packet = (Login)pack;

            var user = packet.Username;
            var pass = packet.Password;

            var halfPass = pass.Length / 2;
            var censoredPass = pass.Substring(0, halfPass) + new string('-', pass.Length - halfPass);

            try
            {
                if (session.Login(user, pass))
                {
                    Program.Logger.InfoFormat("Login success from {0} for '{1}' using password '{2}'", session.RemoteAddress, user, censoredPass);
                    Program.SendSysMessage(session, string.Format("Logged in as {0}.", session.Name));
                }
                else
                {
                    Program.Logger.InfoFormat("Login failed from {0} for '{1}' using password '{2}'", session.RemoteAddress, user, censoredPass);
                    Program.SendSysMessage(session, "Login failed.");
                    return;
                }
            }
            catch (Exception e)
            {
                var exFormat = string.Format("{0}: `{1}`", e.GetType(), e.Message);

                Program.Logger.WarnFormat("Login error from {0} for '{1}' using password '{2}': {3}", session.RemoteAddress, user, censoredPass, exFormat);
                Program.SendSysMessage(session, "Login failed.");
                return;
            }
            
            if (session.Account.Name != "Guest")
                Ticker.Add(session.Account.Name);

            if (!session.HasBacklog)
            {
                Program.SendHistory(session);
                session.HasBacklog = true;
            }

            var o = new Packets.ClientPermissions
            {
                Username = session.Name,
                CanChat = session.Permissions.HasFlag(Permissions.Chat)
            };
            Program.Send(session, o);
        }
    }
}
