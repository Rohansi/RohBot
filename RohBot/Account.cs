using System;
using System.Collections.Generic;
using System.Linq;

namespace RohBot
{
    public class Account
    {
        public long Id { get; private set; }
        public string Address;
        public string Name;
        public string Password;
        public string Salt;
        public string EnabledStyle;
        public string[] Rooms;

        public Account()
        {
            Id = 0;
        }

        internal Account(dynamic row)
        {
            Id = row.id;
            Address = row.address;
            Name = row.name;
            Password = row.password;
            Salt = row.salt;
            EnabledStyle = row.enabledstyle;
            Rooms = row.rooms;
        }

        public void Save()
        {
            if (Id == 0)
                throw new InvalidOperationException("Cannot save row that does not exist");

            var cmd = new SqlCommand("UPDATE rohbot.accounts SET password=:pass, salt=:salt, enabledstyle=:style, rooms=:rooms WHERE id=:id;");
            cmd["id"] = Id;
            cmd["pass"] = Password;
            cmd["salt"] = Salt;
            cmd["style"] = EnabledStyle;
            cmd["rooms"] = Rooms ?? new string[0];
            cmd.ExecuteNonQuery();
        }

        public void Insert()
        {
            if (Id != 0)
                throw new InvalidOperationException("Cannot insert existing row");

            var cmd = new SqlCommand("INSERT INTO rohbot.accounts (address,name,password,salt,enabledstyle,rooms) VALUES (:addr,:name,:pass,:salt,:style,:rooms) RETURNING id;");
            cmd["addr"] = Address;
            cmd["name"] = Name;
            cmd["pass"] = Password;
            cmd["salt"] = Salt;
            cmd["style"] = EnabledStyle;
            cmd["rooms"] = Rooms ?? new string[0];
            Id = (long)cmd.ExecuteScalar();
        }

        public static Account Get(string username)
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.accounts WHERE lower(name)=lower(:name);");
            cmd["name"] = username;
            var row = cmd.Execute().FirstOrDefault();
            return row == null ? null : new Account(row);
        }

        public static IEnumerable<Account> FindWithAddress(string address)
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.accounts WHERE address=:addr;");
            cmd["addr"] = address;
            return cmd.Execute().Select(row => new Account(row));
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
        public long Id { get; private set; }
        public string Name;
        public string Address;
        public string Token;
        public long Created;

        public LoginToken()
        {
            Id = 0;
        }

        internal LoginToken(dynamic row)
        {
            Id = row.id;
            Name = row.name;
            Address = row.address;
            Token = row.token;
            Created = row.created;
        }

        public void Insert()
        {
            if (Id != 0)
                throw new InvalidOperationException("Cannot insert existing row");

            var cmd = new SqlCommand("INSERT INTO rohbot.logintokens (name,address,token,created) VALUES(:name,:addr,:token,:created) RETURNING id;");
            cmd["name"] = Name;
            cmd["addr"] = Address;
            cmd["token"] = Token;
            cmd["created"] = Created;
            Id = (long)cmd.ExecuteScalar();
        }

        public static IEnumerable<LoginToken> FindAll(string username)
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.logintokens WHERE lower(name)=lower(:name);");
            cmd["name"] = username;

            return cmd.Execute().Select(row => new LoginToken(row));
        }

        public static void RemoveOlderThan(long time)
        {
            var cmd = new SqlCommand("DELETE FROM rohbot.logintokens WHERE created<:time;");
            cmd["time"] = time;
            cmd.ExecuteNonQuery();
        }
    }
}
