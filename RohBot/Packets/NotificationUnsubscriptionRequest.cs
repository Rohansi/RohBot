using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RohBot.Packets
{
    public class NotificationUnsubscriptionRequest : Packet
    {
        public override string Type => "notificationUnsubscriptionRequest";

        public string RegexPattern;
        public string DeviceToken;

        public override void Handle(Connection connection)
        {
            var account = connection.Session.Account;
            var notification = Notification.Get(DeviceToken);

            if (notification == null)
            {
                connection.SendSysMessage("Device is not registered for push notifications");
                return;
            }
            else if(notification.UserID != account.Id)
            {
                connection.SendSysMessage("You do not own this device");
                return;
            }

            notification.Remove();

            Program.NotificationManager.InvalidateNotificationCache();

            var notificationSubscription = new NotificationSubscription();
            notificationSubscription.DeviceToken = DeviceToken;
            notificationSubscription.RegexPattern = RegexPattern;
            notificationSubscription.Registered = false;

            connection.Send(notificationSubscription);
        }
    }
}
