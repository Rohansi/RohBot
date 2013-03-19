using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SuperWebSocket;
using log4net;

namespace SteamMobile
{
    [Flags]
    public enum Permissions : ushort
    {
        None = 0,
        Chat = 1 << 0,
        Ban = 1 << 1,

        // Old
        FriendList = 1 << 13,
        ConversationList = 1 << 14,
        OpenChat = 1 << 15,

        All = ushort.MaxValue
    }

    class Session
    {
        private static readonly ILog Logger = LogManager.GetLogger("Steam");

        public readonly WebSocketSession Socket;
        public SteamChat CurrentChat;
        public bool HasBacklog = false;

        public bool Authenticated { get; private set; }
        public string Name;
        public Permissions Permissions;

        public Session(WebSocketSession socket)
        {
            Socket = socket;
            Authenticated = false;
            CurrentChat = null;
        }

        public bool Load(string user, string pass)
        {
            // only allow alphanumeric strings for user
            if (!user.All(char.IsLetterOrDigit))
                return false;

            var file = Path.Combine("accounts/", user.ToLower() + ".json");
            dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(file));

            if (pass != (string)obj.Password || (bool)obj.Banned)
                return false;

            Name = (string)obj.Name;
            Permissions = (Permissions)ushort.Parse((string)obj.Permissions);
            Authenticated = true;
            return true;
        }

        public static string Ban(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                return "No target specified.";

            if (!user.All(char.IsLetterOrDigit))
                return "Account does not exist.";

            var file = Path.Combine("accounts/", user.ToLower() + ".json");
            if (!File.Exists(file))
                return "Account does not exist.";

            dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(file));
            obj.Banned = true;
            File.WriteAllText(file, JsonConvert.SerializeObject(obj));

            return "Account banned.";
        }
    }
}
