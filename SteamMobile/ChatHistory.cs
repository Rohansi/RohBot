using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile
{
    class ChatLine
    {
        public readonly long Date;
        public readonly string Sender;
        public readonly string Message;

        public ChatLine(long date, string sender, string message)
        {
            Date = date;
            Sender = sender;
            Message = message;
        }
    }

    class ChatHistory
    {
        
    }
}
