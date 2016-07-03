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
            var account = connection.Session.Account;
            var notification = new Notification();

            notification.UserID = account.Id;
            notification.Regex = new Regex(RegexPattern);
            notification.DeviceToken = DeviceToken;

            if (Notification.Exists(DeviceToken))
                notification.Save();
            else
            {
                if (Notification.FindWithID(account.Id).Count() < 10)
                    notification.Insert();
                else
                {
                    connection.SendSysMessage("You may only have 10 devices registered for push notifications.");
                    return;
                }
            }

            Program.NotificationManager.InvalidateNotificationCache();

            var notificationSubscription = new NotificationSubscription();
            notificationSubscription.DeviceToken = DeviceToken;
            notificationSubscription.RegexPattern = RegexPattern;
            notificationSubscription.Registered = true;

            connection.Send(notificationSubscription);
        }
    }
}
