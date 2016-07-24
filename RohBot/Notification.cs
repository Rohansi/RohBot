using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RohBot
{
    public class Notification
    {
        public long Id { get; private set; }
        public long UserId { get; set; }
        public Regex Regex { get; set; }
        public string DeviceToken { get; set; }
        public string Name { get; set; }
        public HashSet<string> Rooms { get; set; }
         
        public Notification()
        {

        }

        public Notification(dynamic row)
        {
            Id = row.id;
            UserId = row.userid;
            Regex = new Regex(row.regex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(15));
            DeviceToken = row.devicetoken;
            Name = row.name;
            Rooms = new HashSet<string>((string[])row.rooms);
        }

        public void Insert()
        {
            var cmd = new SqlCommand("INSERT INTO rohbot.notifications (userid, regex, devicetoken) VALUES(:userid, :regex, :devicetoken) RETURNING id;");
            cmd["userid"] = UserId;
            cmd["regex"] = Regex.ToString();
            cmd["devicetoken"] = DeviceToken;

            Id = (long)cmd.ExecuteScalar();
        }

        public void Save()
        {
            var cmd = new SqlCommand("UPDATE rohbot.notifications SET regex=:regex WHERE id=:id");
            cmd["id"] = Id;
            cmd["regex"] = Regex.ToString();

            cmd.ExecuteNonQuery();
        }

        public void Remove()
        {
            var cmd = new SqlCommand("DELETE FROM rohbot.notifications WHERE devicetoken=:devicetoken AND userid=:userid");
            cmd["devicetoken"] = DeviceToken;
            cmd["userid"] = UserId;

            cmd.ExecuteNonQuery();
        }
    }
}
