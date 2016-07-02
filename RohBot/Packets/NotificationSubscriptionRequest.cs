using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RohBot.Packets
{
    public class NotificationSubscriptionRequest : Packet
    {
        public override string Type => "notificationSubscriptionRequest";

        public ICollection<String> Keywords;

        public override void Handle(Connection connection)
        {
            Console.WriteLine("Received notificationSubscriptionRequest from {0}", connection.Session.Account.Name);

            //throw new NotImplementedException();
        }
    }
}
