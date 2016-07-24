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
            var notification = Program.NotificationManager.Get(DeviceToken);

            if (notification == null)
            {
                connection.SendSysMessage("This device is not registered for push notifications.");
                return;
            }

            if (notification.UserId != account.Id)
            {
                connection.SendSysMessage("This device is not registered with your account.");
                return;
            }

            notification.Remove();

            Program.NotificationsDirty = true;

            var notificationSubscription = new NotificationSubscription();
            notificationSubscription.DeviceToken = DeviceToken;
            notificationSubscription.RegexPattern = RegexPattern;
            notificationSubscription.Registered = false;

            connection.Send(notificationSubscription);
        }
    }
}
