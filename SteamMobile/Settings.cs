using System;
using System.Collections.Generic;
using System.Linq;
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
        public static int MaxDataSize { get; private set; }
        public static Dictionary<string, string> Alias { get; private set; }
        public static List<SteamID> CommandIgnore { get; private set; } 
        public static string DefaultChat { get; private set; }
        public static Dictionary<string, SteamID> Chats { get; private set; }

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
            MaxDataSize = (int)settings.MaxDataSize;

            Alias = new Dictionary<string, string>();
            foreach (var kvp in (JObject)settings.Alias)
            {
                Alias.Add(kvp.Key, kvp.Value.Value<string>());
            }

            CommandIgnore = ((JArray)settings.CommandIgnore).Values<string>().Select(s => new SteamID(ulong.Parse(s))).ToList();

            DefaultChat = (string)settings.DefaultChat;

            Chats = new Dictionary<string, SteamID>();
            foreach (var kvp in (JObject)settings.Chats)
            {
                Chats.Add(kvp.Key, new SteamID(ulong.Parse(kvp.Value.Value<string>())));
            }
        }
    }
}
