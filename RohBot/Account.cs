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
        public long UserId;
        public long Created;
        public long Accessed;
        public string UserAgent;
        public string Address;
        public string Token;

        public LoginToken()
        {
            Id = 0;
        }

        internal LoginToken(dynamic row)
        {
            Id = row.id;
            UserId = row.userid;
            Created = row.created;
            Accessed = row.accessed;
            UserAgent = row.useragent;
            Address = row.address;
            Token = row.token;
        }

        public void Insert()
        {
            if (Id != 0)
                throw new InvalidOperationException("Cannot insert existing row");

            var cmd = new SqlCommand(@"
                INSERT INTO rohbot.logintokens2 (userid,created,accessed,useragent,address,token)
                VALUES(:userid,:created,:accessed,:useragent,:address,:token) RETURNING id;");

            cmd["userid"] = UserId;
            cmd["created"] = Created;
            cmd["accessed"] = Accessed;
            cmd["useragent"] = UserAgent;
            cmd["address"] = Address;
            cmd["token"] = Token;
            Id = (long)cmd.ExecuteScalar();
        }

        public void UpdateAccessed(string userAgent, string address)
        {
            if (Id == 0)
                throw new InvalidOperationException("Cannot update non-existing row");

            var now = Util.GetCurrentTimestamp();
            var cmd = new SqlCommand("UPDATE rohbot.logintokens2 SET accessed=:accessed, useragent=:useragent, address=:address WHERE id=:id;");
            cmd["id"] = Id;
            cmd["accessed"] = now;
            cmd["useragent"] = userAgent;
            cmd["address"] = address;
            cmd.ExecuteNonQuery();

            Accessed = now;
            UserAgent = userAgent;
            Address = address;
        }

        public static IEnumerable<LoginToken> FindAll(long userId)
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.logintokens2 WHERE userid=:userid ORDER BY accessed DESC;");
            cmd["userid"] = userId;

            return cmd.Execute().Select(row => new LoginToken(row));
        }

        public static LoginToken Find(long userId, string token)
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.logintokens2 WHERE userid=:userid AND token=:token;");
            cmd["userid"] = userId;
            cmd["token"] = token;

            return cmd.Execute().Select(row => new LoginToken(row)).SingleOrDefault();
        }

        public static void RemoveAll(long userId)
        {
            var cmd = new SqlCommand("DELETE FROM rohbot.logintokens2 WHERE userid=:userid;");
            cmd["userid"] = userId;
            cmd.ExecuteNonQuery();
        }

        public static void RemoveOlderThan(long time)
        {
            var cmd = new SqlCommand("DELETE FROM rohbot.logintokens2 WHERE accessed<:time;");
            cmd["time"] = time;
            cmd.ExecuteNonQuery();
        }
    }
}
