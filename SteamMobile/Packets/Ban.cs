using System;

namespace SteamMobile.Packets
{
    public class Ban : Packet
    {
        public override string Type { get { return "ban"; } }

        public string Target = null;

        public static void Handle(Session session, Packet pack)
        {
            var packet = (Ban)pack;

            if (!session.Permissions.HasFlag(SteamMobile.Permissions.Ban))
                return;

            try
            {
                Program.Logger.InfoFormat("User '{0}' banning '{1}'", session.Name, packet.Target);

                var res = Program.Ban(packet.Target);
                Program.SendMessage(session, "*", res);
            }
            catch (Exception)
            {
                Program.SendMessage(session, "*", "Failed to ban. Check logs.");
                throw;
            }
        }
    }
}
