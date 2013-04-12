using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SteamKit2;

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

    public class Account
    {
        public string Username { get; private set; }
        public string Password { get; private set; }

        public string Name { get; private set; }
        public SteamID Id { get; private set; }

        public Permissions Permissions { get; private set; }
        public bool Banned { get; set; }

        public string Reply = null;

        private Account() { }

        public void Save()
        {
            var file = Path.Combine("accounts/", Username.ToLower() + ".json");
            dynamic acc = new
            {
                Name,
                Password,
                Id = Id.ConvertToUInt64().ToString(),
                Permissions,
                Banned
            };

            File.WriteAllText(file, JsonConvert.SerializeObject(acc));
        }

        public static Account Load(string name)
        {
            Account res;

            // Only allowed letters, digits and spaces
            if (!name.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)))
                return null;

            var file = Path.Combine("accounts/", name.ToLower() + ".json");

            try
            {
                dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(file));

                res = new Account()
                {
                    Username = name,
                    Password = (string)obj.Password,

                    Name = (string)obj.Name,
                    Id = new SteamID(ulong.Parse((string)obj.Id)),

                    Permissions = (Permissions)ushort.Parse((string)obj.Permissions),
                    Banned = (bool)obj.Banned
                };
            }
            catch (Exception e)
            {
                Program.Logger.ErrorFormat("Error loading account '{0}': {1}", name, e);
                return null;
            }

            return res;
        }
    }

    public static class Accounts
    {
        private static Dictionary<string, Account> accounts;

        static Accounts()
        {
            Reload();
        }
         
        public static Account Get(string username)
        {
            username = username.ToLower();

            Account res;
            return accounts.TryGetValue(username, out res) ? res : null;
        }

        public static Account Find(string name)
        {
            name = name.ToLower();
            return accounts.Values.FirstOrDefault(a => a.Username.ToLower() == name || a.Name.ToLower() == name);
        }

        public static Account Find(SteamID id)
        {
            return accounts.Values.FirstOrDefault(a => a.Id == id);
        }

        public static void Reload()
        {
            accounts = new Dictionary<string, Account>();

            foreach (var file in Directory.GetFiles("accounts/"))
            {
                var accountName = Path.GetFileNameWithoutExtension(file);
                var account = Account.Load(accountName);

                if (account == null)
                    continue;

                accounts.Add(accountName.ToLower(), account);
            }
        }
    }
}
