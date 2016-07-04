using System;

namespace RohBot.Packets
{
    public class NotificationSubscription : Packet
    {
        public override string Type => "notificationSubscription";

        public string RegexPattern;
        public string DeviceToken;
        public bool Registered;

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
