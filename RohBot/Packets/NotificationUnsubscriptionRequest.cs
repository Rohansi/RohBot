namespace RohBot.Packets
{
    public class NotificationUnsubscriptionRequest : Packet
    {
        public override string Type => "notificationUnsubscriptionRequest";

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
            
            var notification = Program.NotificationManager.Get(DeviceToken);
            if (notification == null)
            {
                connection.SendSysMessage("This device is not registered for push notifications.");
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
