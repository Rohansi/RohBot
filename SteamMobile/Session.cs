using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        BanProof = 1 << 2,

        Admin = 1 << 15,

        All = ushort.MaxValue
    }

    public class Session
    {
        public readonly WebSocketSession Socket;
        public bool HasBacklog = false;

        public bool Authenticated { get; private set; }
        public string Name;
        public Permissions Permissions;

        public Session(WebSocketSession socket)
        {
            Socket = socket;
            Authenticated = false;
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

        public static bool Ban(string user, out string response)
        {
            if (string.IsNullOrWhiteSpace(user) || !user.All(char.IsLetterOrDigit))
            {
                response = "Account does not exist.";
                return false;
            }

            var file = Path.Combine("accounts/", user.ToLower() + ".json");
            if (!File.Exists(file))
            {
                response = "Account does not exist.";
                return false;
            }

            dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(file));
            var permissions = (Permissions)ushort.Parse((string)obj.Permissions);

            if (permissions.HasFlag(Permissions.BanProof))
            {
                response = "Account can not be banned.";
                return false;
            }

            obj.Banned = true;
            File.WriteAllText(file, JsonConvert.SerializeObject(obj));

            response = "Account banned.";
            return true;
        }
    }
}
