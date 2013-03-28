using System;
using System.Net;

namespace SteamMobile
{
    public abstract class HistoryLine
    {
        public abstract string Type { get; }
        public readonly long Date;
        public readonly string Content;

        protected HistoryLine(long date, string content)
        {
            Date = date;
            Content = WebUtility.HtmlEncode(content);
        }
    }

    public class ChatLine : HistoryLine
    {
        public override string Type { get { return "chat"; } }

        public readonly string Sender;

        public ChatLine(long date, string sender, string content)
            : base(date, content)
        {
            Sender = WebUtility.HtmlEncode(sender);
        }
    }

    public class StatusLine : HistoryLine
    {
        public override string Type { get { return "state"; } }

        public StatusLine(long date, string content)
            : base(date, content)
        {
            
        }
    }

    public class WhisperLine : HistoryLine
    {
        public override string Type { get { return "whisper"; } }

        public readonly string Sender;
        public readonly string Receiver;

        public WhisperLine(long date, string sender, string receiver, string content)
            : base(date, content)
        {
            Sender = WebUtility.HtmlEncode(sender);
            Receiver = WebUtility.HtmlEncode(receiver);
        }
    }
}
