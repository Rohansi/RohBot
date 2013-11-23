using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;

namespace SteamMobile
{
    public class Account
    {
        public ObjectId Id;
        public string Address;
        public string Name;
        public string NameLower;
        public byte[] Password;
        public byte[] Salt;
        public string DefaultRoom;

        [BsonDefaultValue("")]
        public string EnabledStyle = "";

        public static Account Get(string username)
        {
            username = username.ToLower();
            return Database.Accounts.AsQueryable().FirstOrDefault(a => a.NameLower == username);
        }

        public static bool Exists(string username)
        {
            return Get(username) != null;
        }

        public class Comparer : IEqualityComparer<Account>
        {
            public bool Equals(Account x, Account y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(Account obj)
            {
                if (ReferenceEquals(obj, null))
                    return 0;
                return obj.Id.GetHashCode();
            }
        }
    }

    public class LoginToken
    {
        public ObjectId Id;
        public string Name;
        public string Address;
        public string Token;
        public long Created;
    }
}
