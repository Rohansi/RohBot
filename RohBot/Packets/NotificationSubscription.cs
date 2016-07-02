using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RohBot.Packets
{
    public class NotificationSubscription : Packet
    {
        public override string Type => "notificationSubscription";

        public List<string> NotificationKeywords;

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
