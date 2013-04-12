using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SuperWebSocket;

namespace SteamMobile
{
    public class Session
    {
        public readonly WebSocketSession Socket;
        public string Username { get; private set; }

        public bool HasBacklog = false;

        public bool Authenticated
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Username);
            }
        }

        public string Name
        {
            get
            {
                var account = Accounts.Get(Username);
                return account != null ? account.Name : "NOLOGIN";
            }
        }

        public Permissions Permissions
        {
            get
            {
                var account = Accounts.Get(Username);
                return account != null ? account.Permissions : Permissions.None;
            }
        }

        public Session(WebSocketSession socket)
        {
            Socket = socket;
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
            return true;
        }

        public static bool Ban(string user, out string response)
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

            account.Banned = true;
            account.Save();

            response = "Account banned.";
            return true;
        }
    }
}
