using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RohBot
{
    public class Notification
    {
        public long ID { get; private set; }
        public long UserID { get; set; }
        public Regex Regex { get; set; }
        public string DeviceToken { get; set; }

        public Notification()
        {

        }

        public Notification(dynamic row)
        {
            ID = row.id;
            UserID = row.userid;
            Regex = new Regex(row.regex);
            DeviceToken = row.devicetoken;
        }

        public void Insert()
        {
            var cmd = new SqlCommand("INSERT INTO rohbot.notifications (id, userid, regex, devicetoken) VALUES(:id, :userid, :regex, :devicetoken) RETURNING id;");
            cmd["id"] = ID;
            cmd["userid"] = UserID;
            cmd["regex"] = Regex.ToString();
            cmd["devicetoken"] = DeviceToken;

            ID = (long) cmd.ExecuteScalar();
        }

        public void Save()
        {
            var cmd = new SqlCommand("UPDATE rohbot.notifications SET regex=:regex WHERE id=:id");
            cmd["id"] = ID;
            cmd["regex"] = Regex.ToString();

            cmd.ExecuteNonQuery();
        }

        public static bool Exists(string deviceToken)
        {
            return Program.NotificationManager.Notifications
                                                    .Where(n => n.DeviceToken == deviceToken)
                                                    .Any();
        }

        public static IEnumerable<Notification> FindWithID(long userID)
        {
            return Program.NotificationManager.Notifications
                                                    .Where(n => n.UserID == userID);
        }
    }
}
