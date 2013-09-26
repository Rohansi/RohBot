using System.Collections.Generic;
using MongoDB.Bson;

namespace SteamMobile
{
    public class Account
    {
        public ObjectId Id;
        public string Name;
        public string NameLower;
        public string SteamId;
        public string DefaultRoom;

        public class Comparer : IEqualityComparer<Account>
        {
            public bool Equals(Account x, Account y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(Account obj)
            {
                if (ReferenceEquals(obj, null))
                    return 0;
                return obj.Name.GetHashCode();
            }
        }
    }

    public class LoginToken
    {
        public ObjectId Id;
        public string Name;
        public string Address;
        public string Token;
    }
}
