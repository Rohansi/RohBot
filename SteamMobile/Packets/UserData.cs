using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SteamMobile.Packets
{
    public class UserData : Packet
    {
        public override string Type { get { return "userData"; } }

        /// <summary>
        /// Possible values:
        ///   "store"   - Store data on the server.
        ///   "load"    - Request data from the server.
        ///   "loaded"  - Response to load. Contains stored data.
        /// </summary>
        public string Action;

        public string Data;

        public static void Handle(Session session, Packet pack)
        {
            var packet = (UserData)pack;

            switch (packet.Action)
            {
                case "store":
                    StoreData(session, packet.Data);
                    break;
                case "load":
                    LoadData(session);
                    break;
            }
        }

        private static void StoreData(Session session, string data)
        {
            if (data.Length > Settings.MaxDataSize)
            {
                Program.SendSysMessage(session, "UserData too large");
                return;
            }

            try
            {
                var file = Path.Combine("userdata/", session.Name.ToLower() + ".txt");
                File.WriteAllText(file, data, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Program.Logger.Error("Failed to store UserData", e);
                Program.SendSysMessage(session, "Failed to store UserData");
            }
        }

        private static void LoadData(Session session)
        {
            try
            {
                var file = Path.Combine("userdata/", session.Name.ToLower() + ".txt");
                var data = File.Exists(file) ? File.ReadAllText(file, Encoding.UTF8) : "";

                var pack = new UserData
                {
                    Action = "loaded",
                    Data = data
                };
                Program.Send(session, pack);
            }
            catch (Exception e)
            {
                Program.Logger.Error("Failed to load UserData", e);
                Program.SendSysMessage(session, "Failed to load UserData");
            }
        }
    }
}
