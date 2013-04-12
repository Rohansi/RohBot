using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace SteamMobile
{
    static class Settings
    {
        public static string Username { get; private set; }
        public static string Password { get; private set; }
        public static string PersonaName { get; private set; }

        public static SteamID ChatId { get; private set; }

        public static int MaxDataSize { get; private set; }

        public static Dictionary<string, string> Alias { get; private set; }

        static Settings()
        {
            Reload();
        }

        public static void Reload()
        {
            dynamic settings = JsonConvert.DeserializeObject(File.ReadAllText("settings.json"));

            Username = (string)settings.Username;
            Password = (string)settings.Password;
            PersonaName = (string)settings.PersonaName;

            ChatId = new SteamID(ulong.Parse((string)settings.ChatId));

            MaxDataSize = (int)settings.MaxDataSize;

            Alias = new Dictionary<string, string>();
            foreach (var kvp in (JObject)settings.Alias)
            {
                Alias.Add(kvp.Key, kvp.Value.Value<string>());
            }
        }
    }
}
