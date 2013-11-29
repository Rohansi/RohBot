using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PostgresMigrate
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
}
