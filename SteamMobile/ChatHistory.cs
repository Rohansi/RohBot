using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile
{
    public class ChatLine
    {
        public readonly long Date;
        public readonly string Sender;
        public readonly string Content;

        public ChatLine(long date, string sender, string content)
        {
            Date = date;
            Sender = sender;
            Content = content;
        }
    }
}
