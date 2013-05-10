using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fleck;
using Newtonsoft.Json;

namespace SteamMobile
{
    public class Session
    {
        public readonly IWebSocketConnection Socket;
        public readonly string RemoteAddress;

        public Account Account
        {
            get { return Accounts.Get(Username ?? ""); }
        }

        public string Username { get; private set; }

        public bool Authenticated
        {
            get { return !string.IsNullOrWhiteSpace(Username); }
        }

        public string Name
        {
            get { return Account != null ? Account.Name : "NOLOGIN"; }
        }

        public Permissions Permissions
        {
            get { return Account != null ? Account.Permissions : Permissions.None; }
        }

        public bool HasBacklog = false;
        public string Chat = Settings.DefaultChat;

        public Session(IWebSocketConnection socket)
        {
            Socket = socket;
            RemoteAddress = socket.ConnectionInfo.ClientIpAddress; // using modified Fleck for this, uses X-Real-IP if needed
            Username = "";
        }

        public bool Login(string user, string pass)
        {
            var account = Accounts.Get(user);

            if (account == null)
            {
                Program.Logger.Info("Account null");
                return false;
            }

            if (pass != account.Password || account.Banned)
            {
                Program.Logger.Info("Bad pass or banned");
                return false;
            }

            Username = user;
            Chat = account.DefaultChat;

            if (!Program.Chats.ContainsKey(Chat))
            {
                Chat = Settings.DefaultChat;
                account.DefaultChat = Chat;
                account.Save();
            }

            return true;
        }

        public static bool Ban(string user, bool banned, out string response)
        {
            var account = Accounts.Find(user);
            
            if (account == null)
            {
                response = "Account does not exist.";
                return false;
            }

            if (account.Permissions.HasFlag(Permissions.BanProof))
            {
                response = "Account can not be banned.";
                return false;
            }

            account.Banned = banned;
            account.Save();

            response = banned ? "Account banned." : "Account unbanned.";
            return true;
        }
    }
}
