using System;
using Newtonsoft.Json;

namespace RohBot
{
    public abstract class HistoryLine
    {
        public abstract string Type { get; }

        [JsonIgnore]
        public long Id { get; protected set; }

        public long Date;
        public string Chat;
        public string Content;

        protected HistoryLine() { }

        protected HistoryLine(long date, string chat, string content)
        {
            Date = date;
            Chat = chat;
            Content = Util.HtmlEncode(Util.TrimZalgoCharacters(content));
        }

        public static HistoryLine Read(dynamic row)
        {
            switch ((string)row.type)
            {
                case "chat":
                    return new ChatLine
                    {
                        Id = row.id,
                        Date = row.date,
                        Chat = row.chat,
                        Content = row.content,
                        UserType = row.usertype,
                        Sender = row.sender,
                        SenderId = row.senderid,
                        SenderStyle = row.senderstyle,
                        InGame = row.ingame
                    };

                case "state":
                    return new StateLine
                    {
                        Id = row.id,
                        Date = row.date,
                        Chat = row.chat,
                        Content = row.content,
                        State = row.state,
                        For = row.@for,
                        ForId = row.forid,
                        ForType = row.fortype,
                        ForStyle = row.forstyle.Equals(DBNull.Value) ? "" : row.forstyle,
                        By = row.by,
                        ById = row.byid,
                        ByType = row.bytype,
                        ByStyle = row.bystyle.Equals(DBNull.Value) ? "" : row.bystyle
                    };

                default:
                    throw new NotSupportedException("Cannot read HistoryLine type: " + row.type);
            }
        }

        public abstract void Insert();
    }

    public class ChatLine : HistoryLine
    {
        public override string Type => "chat";

        public string UserType;
        public string Sender;
        public string SenderId;
        public string SenderStyle;
        public bool InGame;

        public ChatLine() { }

        public ChatLine(
            long date, string chat, string userType,
            string sender, string senderId, string senderStyle,
            string content, bool inGame)
            : base(date, chat, content)
        {
            UserType = userType;
            Sender = Util.HtmlEncode(Util.TrimZalgoCharacters(sender));
            SenderId = senderId;
            SenderStyle = senderStyle;
            InGame = inGame;
        }

        public override void Insert()
        {
            if (Id != 0)
                throw new InvalidOperationException("Cannot insert existing row");

            var cmd = new SqlCommand("INSERT INTO rohbot.chathistory (type,date,chat,content,usertype,sender,senderid,senderstyle,ingame)" +
                                     "VALUES (:type,:date,:chat,:content,:usertype,:sender,:senderid,:senderstyle,:ingame) RETURNING id;");
            cmd["type"] = Type;
            cmd["date"] = Date;
            cmd["chat"] = Chat;
            cmd["content"] = Content;
            cmd["usertype"] = UserType;
            cmd["sender"] = Sender;
            cmd["senderid"] = SenderId;
            cmd["senderstyle"] = SenderStyle;
            cmd["ingame"] = InGame;
            Id = (long)cmd.ExecuteScalar();
        }
    }

    public class StateLine : HistoryLine
    {
        public override string Type => "state";

        public string State;
        public string For;
        public string ForId;
        public string ForType;
        public string ForStyle;
        public string By;
        public string ById;
        public string ByType;
        public string ByStyle;

        public StateLine() { }

        public StateLine(
            long date, string chat, string state,
            string @for, string forId, string forType, string forStyle,
            string by, string byId, string byType, string byStyle, string content)
            : base(date, chat, content)
        {
            State = state;
            For = Util.HtmlEncode(Util.TrimZalgoCharacters(@for));
            ForId = forId;
            ForType = forType;
            ForStyle = forStyle;
            By = Util.HtmlEncode(Util.TrimZalgoCharacters(by));
            ById = byId;
            ByType = byType;
            ByStyle = byStyle;
        }

        public override void Insert()
        {
            if (Id != 0)
                throw new InvalidOperationException("Cannot insert existing row");

            var cmd = new SqlCommand("INSERT INTO rohbot.chathistory (type,date,chat,content,state,\"for\",forid,fortype,forstyle,by,byid,bytype,bystyle)" + 
                                     "VALUES (:type,:date,:chat,:content,:state,:for,:forid,:fortype,:forstyle,:by,:byid,:bytype,:bystyle) RETURNING id;");
            cmd["type"] = Type;
            cmd["date"] = Date;
            cmd["chat"] = Chat;
            cmd["content"] = Content;
            cmd["state"] = State;
            cmd["for"] = For;
            cmd["forid"] = ForId;
            cmd["fortype"] = ForType;
            cmd["forstyle"] = ForStyle;
            cmd["by"] = By;
            cmd["byid"] = ById;
            cmd["bytype"] = ByType;
            cmd["bystyle"] = ByStyle;
            Id = (long)cmd.ExecuteScalar();
        }
    }
}
