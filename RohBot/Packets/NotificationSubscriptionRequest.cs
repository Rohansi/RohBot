using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RohBot.Packets
{
    public class NotificationSubscriptionRequest : Packet
    {
        public override string Type => "notificationSubscriptionRequest";

        public string RegexPattern;
        public string DeviceToken;

        public override void Handle(Connection connection)
        {
            Console.WriteLine("Received notificationSubscriptionRequest from {0}", connection.Session.Account.Name);

            var account = connection.Session.Account;
            var notification = new Notification();

            notification.UserID = account.Id;
            notification.Regex = new Regex(RegexPattern);
            notification.DeviceToken = DeviceToken;
            
            if (Notification.Exists(DeviceToken))
                notification.Save();
            else
                notification.Insert();

            Program.NotificationManager.InvalidateNotificationCache();
        }
    }
}
