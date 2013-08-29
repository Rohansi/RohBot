using System;
using System.Net;

namespace SteamMobile
{
    public abstract class HistoryLine
    {
        public abstract string Type { get; }

        public readonly long Date;
        public readonly string Chat;
        public readonly string Content;

        protected HistoryLine(long date, string chat, string content, bool encode = true)
        {
            Date = date;
            Chat = chat;
            Content = HtmlEncode(encode, content);
        }

        protected static string HtmlEncode(bool doStuff, string value)
        {
            return doStuff ? WebUtility.HtmlEncode(value) : value;
        }
    }

    public class ChatLine : HistoryLine
    {
        public override string Type { get { return "chat"; } }

        public readonly string UserType;
        public readonly string Sender;
        public readonly string SenderId;
        public readonly bool InGame;

        public ChatLine(long date, string chat, string content, string userType, string sender, string senderId, bool inGame, bool encode = true)
            : base(date, chat, content, encode)
        {
            UserType = HtmlEncode(encode, userType);
            Sender = HtmlEncode(encode, sender);
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

        public StateLine(long date, string chat, string content, string state, string @for, string forId, string by, string byId, bool encode = true)
            : base(date, chat, content, encode)
        {
            State = HtmlEncode(encode, state);
            For = HtmlEncode(encode, @for);
            ForId = forId;
            By = HtmlEncode(encode, by);
            ById = byId;
        }
    }

    public class WhisperLine : HistoryLine
    {
        public override string Type { get { return "whisper"; } }

        public readonly string Sender;
        public readonly string Receiver;

        public WhisperLine(long date, string content, string sender, string receiver, bool encode = true)
            : base(date, "whisper", content, encode)
        {
            Sender = HtmlEncode(encode, sender);
            Receiver = HtmlEncode(encode, receiver);
        }
    }
}
