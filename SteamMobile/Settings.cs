using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SteamMobile
{
    public class Settings
    {
        public string Username;
        public string Password;
        public string PersonaName;
        public string Host;
        public ulong Admin;

        public string DbServer;
        public string DbName;
        public string DbUser;
        public string DbPass;

        public string DefaultRoom;
        public List<RoomInfo> Rooms;

        public static Settings Load(string fileName)
        {
            return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(fileName));
        }
    }
}
