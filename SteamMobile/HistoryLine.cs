using System;
using System.Net;
using EzSteam;

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

        public readonly string UserType;
        public readonly string Sender;
        public readonly string SenderId;
        public readonly bool InGame;

        public ChatLine(long date, string userType, string sender, string senderId, string content, bool inGame)
            : base(date, content)
        {
            UserType = WebUtility.HtmlEncode(userType);
            Sender = WebUtility.HtmlEncode(sender);
            SenderId = senderId;
            InGame = inGame;
        }
    }

    public class StateLine : HistoryLine
    {
        public override string Type { get { return "state"; } }

        public readonly string State;
        public readonly string For;
        public readonly string ForId;
        public readonly string By;
        public readonly string ById;

        public StateLine(long date, string state, string @for, string forId, string by, string byId, string content)
            : base(date, content)
        {
            State = WebUtility.HtmlEncode(state);
            For = WebUtility.HtmlEncode(@for);
            ForId = forId;
            By = WebUtility.HtmlEncode(by);
            ById = byId;
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
