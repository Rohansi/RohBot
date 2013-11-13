using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace SteamMobile
{
    [BsonKnownTypes(typeof(ChatLine), typeof(StateLine), typeof(WhisperLine))]
    public abstract class HistoryLine
    {
        public abstract string Type { get; }

        [JsonIgnore]
        public ObjectId Id;

        public long Date;
        public string Chat;
        public string Content;

        protected HistoryLine(long date, string chat, string content)
        {
            Date = date;
            Chat = chat;
            Content = Util.HtmlEncode(content);
        }
    }

    public class ChatLine : HistoryLine
    {
        public override string Type { get { return "chat"; } }

        public string UserType;
        public string Sender;
        public string SenderId;
        public string SenderStyle;
        public bool InGame;

        public ChatLine(long date, string chat, string userType, string sender, string senderId, string senderStyle, string content, bool inGame)
            : base(date, chat, content)
        {
            UserType = Util.HtmlEncode(userType);
            Sender = Util.HtmlEncode(sender);
            SenderId = senderId;
            SenderStyle = senderStyle;
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
            State = Util.HtmlEncode(state);
            For = Util.HtmlEncode(@for);
            ForId = forId;
            By = Util.HtmlEncode(by);
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
            Sender = Util.HtmlEncode(sender);
            Receiver = Util.HtmlEncode(receiver);
        }
    }
}
