using System;

namespace SteamMobile.Packets
{
    public class Login : Packet
    {
        public override string Type { get { return "login"; } }

        public string Username = null;
        public string Password = null;

        public static void Handle(Session session, Packet pack)
        {
            var packet = (Packets.Login)pack;

            if (Steam.Status != Steam.ConnectionStatus.Connected)
            {
                Program.SendSysMessage(session.Socket, "RohPod is not connected to Steam.");
                session.Socket.CloseWithHandshake("");
                return;
            }

            var user = packet.Username;
            var pass = packet.Password;

            var halfPass = pass.Length / 2;
            var censoredPass = pass.Substring(0, halfPass) + new string('-', pass.Length - halfPass);

            try
            {
                if (session.Load(user, pass))
                {
                    Program.Logger.InfoFormat("Login success from {0} for '{1}' using password '{2}'", session.Socket.RemoteEndPoint, user, censoredPass);
                    Program.SendSysMessage(session.Socket, string.Format("Logged in as {0}.", session.Name));
                }
                else
                {
                    Program.Logger.InfoFormat("Login failed from {0} for '{1}' using password '{2}'", session.Socket.RemoteEndPoint, user, censoredPass);
                    Program.SendSysMessage(session.Socket, "Login failed.");
                }
            }
            catch (Exception e)
            {
                var exFormat = string.Format("{0}: `{1}`", e.GetType(), e.Message);

                Program.Logger.WarnFormat("Login error from {0} for '{1}' using password '{2}': {3}", session.Socket.RemoteEndPoint, user, censoredPass, exFormat);
                Program.SendSysMessage(session.Socket, "Login failed.");
            }

            if (Program.MainChat == null)
            {
                Program.SendSysMessage(session.Socket, "RohPod is not in its chat room.");
                return;
            }

            if (!session.HasBacklog)
            {
                Program.SendHistory(session.Socket);
                session.HasBacklog = true;
            }

            var o = new Packets.ClientPermissions
            {
                Username = session.Name,
                CanChat = session.Permissions.HasFlag(Permissions.Chat)
            };
            Program.Send(session.Socket, o);
        }
    }
}
