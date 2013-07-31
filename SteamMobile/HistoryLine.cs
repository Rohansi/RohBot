using System;
using System.Net;
using EzSteam;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SteamMobile
{
    [BsonKnownTypes(typeof(ChatLine), typeof(StateLine), typeof(WhisperLine))]
    public abstract class HistoryLine
    {
        public abstract string Type { get; }

        public ObjectId Id;
        public long Date;
        public string Chat;
        public string Content;

        protected HistoryLine(long date, string chat, string content)
        {
            Date = date;
            Chat = chat;
            Content = WebUtility.HtmlEncode(content);
        }
    }

    public class ChatLine : HistoryLine
    {
        public override string Type { get { return "chat"; } }

        public string UserType;
        public string Sender;
        public string SenderId;
        public bool InGame;

        public ChatLine(long date, string chat, string userType, string sender, string senderId, string content, bool inGame)
            : base(date, chat, content)
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

        public string State;
        public string For;
        public string ForId;
        public string By;
        public string ById;

        public StateLine(long date, string chat, string state, string @for, string forId, string by, string byId, string content)
            : base(date, chat, content)
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

        public string Sender;
        public string Receiver;

        public WhisperLine(long date, string sender, string receiver, string content)
            : base(date, "whisper", content)
        {
            Sender = WebUtility.HtmlEncode(sender);
            Receiver = WebUtility.HtmlEncode(receiver);
        }
    }
}
