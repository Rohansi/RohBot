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
            if (Program.DelayManager.AddAndCheck(connection, DelayManager.Database))
                return;

            if (connection.Session == null)
            {
                connection.SendSysMessage("You need to be logged in to do that.");
                return;
            }

            if (string.IsNullOrWhiteSpace(RegexPattern) || string.IsNullOrWhiteSpace(DeviceToken))
            {
                connection.SendSysMessage("Missing device token or regex pattern.");
                return;
            }

            if (!IsRegexPatternValid(RegexPattern))
            {
                connection.SendSysMessage("Invalid regex pattern.");
                return;
            }

            var account = connection.Session.Account;
            if (Program.NotificationManager.FindWithId(account.Id).Count() >= 5)
            {
                connection.SendSysMessage("You may only have 5 devices registered for push notifications.");
                return;
            }

            if (Program.NotificationManager.Exists(DeviceToken, out var notification))
            {
                notification.Regex = Notification.CreateRegex(RegexPattern);
                notification.Save();
            }
            else
            {
                notification = new Notification
                {
                    UserId = account.Id,
                    Regex = Notification.CreateRegex(RegexPattern),
                    DeviceToken = DeviceToken
                };

                notification.Insert();
            }

            Program.NotificationsDirty = true;

            var notificationSubscription = new NotificationSubscription();
            notificationSubscription.DeviceToken = DeviceToken;
            notificationSubscription.RegexPattern = RegexPattern;
            notificationSubscription.Registered = true;

            connection.Send(notificationSubscription);
        }

        private static bool IsRegexPatternValid(string regexPattern)
        {
            if (string.IsNullOrWhiteSpace(regexPattern) || regexPattern.Length < 3 || regexPattern.Length > 100)
                return false;

            try
            {
                var x = new Regex(regexPattern);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
