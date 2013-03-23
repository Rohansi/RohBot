using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                Program.SendMessage(session.Socket, "*", "RohPod is not connected to Steam.");
                session.Socket.CloseWithHandshake("");
                return;
            }

            var user = packet.Username;
            var pass = packet.Password;

            try
            {
                if (session.Load(user, pass))
                {
                    Program.Logger.InfoFormat("Login success from {0} for '{1}' using password '{2}'", session.Socket.RemoteEndPoint, user, pass);
                    Program.SendMessage(session.Socket, "*", string.Format("Logged in as {0}.", session.Name));
                }
                else
                {
                    Program.Logger.InfoFormat("Login failed from {0} for '{1}' using password '{2}'", session.Socket.RemoteEndPoint, user, pass);
                    Program.SendMessage(session.Socket, "*", "Login failed.");
                }
            }
            catch (Exception e)
            {
                var exFormat = string.Format("{0}: `{1}`", e.GetType(), e.Message);

                Program.Logger.WarnFormat("Login error from {0} for '{1}' using password '{2}': {3}", session.Socket.RemoteEndPoint, user, pass, exFormat);
                Program.SendMessage(session.Socket, "*", "Login failed.");
            }

            if (Program.MainChat == null)
            {
                Program.SendMessage(session.Socket, "*", "RohPod is not in its chatroom.");
                return;
            }

            if (!session.HasBacklog)
            {
                Program.SendHistory(session.Socket);
                session.HasBacklog = true;
            }

            var o = new Packets.ClientPermissions
            {
                CanChat = session.Permissions.HasFlag(Permissions.Chat)
            };
            Program.Send(session.Socket, o);
        }
    }
}
