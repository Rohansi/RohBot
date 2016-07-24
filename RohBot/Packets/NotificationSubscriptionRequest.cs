using System.Linq;
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

            notification.UserId = account.Id;
            notification.Regex = new Regex(RegexPattern);
            notification.DeviceToken = DeviceToken;

            if (Program.NotificationManager.Exists(DeviceToken))
            {
                notification.Save();
            }
            else
            {
                if (Program.NotificationManager.FindWithId(account.Id).Count() < 5)
                {
                    notification.Insert();
                }
                else
                {
                    connection.SendSysMessage("You may only have 5 devices registered for push notifications.");
                    return;
                }
            }

            Program.NotificationsDirty = true;

            var notificationSubscription = new NotificationSubscription();
            notificationSubscription.DeviceToken = DeviceToken;
            notificationSubscription.RegexPattern = RegexPattern;
            notificationSubscription.Registered = true;

            connection.Send(notificationSubscription);
        }
    }
}
