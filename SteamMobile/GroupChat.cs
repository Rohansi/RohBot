using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EzSteam;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamMobile
{
    public class GroupChat
    {
        public readonly string Name;
        public readonly SteamID RoomId;
        public Chat Chat;
        public IEnumerable<HistoryLine> History
        {
            get { return history; }
        }

        private readonly LinkedList<HistoryLine> history;
        public bool Active { get; private set; }

        public GroupChat(string name, SteamID roomId)
        {
            Name = name;
            RoomId = roomId;
            Active = true;
            history = new LinkedList<HistoryLine>();
        }

        public void Update()
        {
            if (!Active)
            {
                if (Chat != null)
                {
                    Chat.Leave(ChatLeaveReason.Left);
                    Chat = null;
                }

                return;
            }

            if (Chat != null)
                return;

            Chat = Steam.Bot.Join(RoomId);
            Chat.EchoSelf = true;
            Chat.OnEnter += sender => Program.Logger.Info("Entered " + Name);
            Chat.OnLeave += (sender, reason) =>
            {
                Program.Logger.Info("Left " + Name + ": " + reason);
                Chat = null;
            };

            Chat.OnMessage += HandleMessage;
            Chat.OnUserEnter += HandleEnter;
            Chat.OnUserLeave += HandleLeave;
        }

        public void Send(string message)
        {
            if (Chat != null)
                Chat.Send(message);
        }

        public void Leave()
        {
            if (Chat != null)
            {
                Chat.Leave(ChatLeaveReason.Left);
                Chat = null;
            }

            Active = false;
        }

        private void HandleMessage(Chat sender, Persona messageSender, string message)
        {
            var senderName = Steam.GetName(messageSender.Id);
            var senderType = "Steam";
            var senderId = messageSender.Id.ConvertToUInt64().ToString();
            var inGame = false;

            if (messageSender.Id == Steam.Bot.PersonaId && message.StartsWith("["))
            {
                senderType = "RohBot";
                var nameEnd = message.IndexOf(']');
                senderName = message.Substring(1, nameEnd - 1);
                senderId = senderName.GetHashCode().ToString();
                message = message.Substring(nameEnd + 2);
            }
            else
            {
                inGame = messageSender.Playing != null && messageSender.Playing.AppID != 0;
            }

            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), senderType, senderName, senderId, message, inGame);
            AddHistory(line);

            foreach (var session in Program.Sessions.Values.ToList())
            {
                if (session.Chat == Name)
                    Program.SendHistoryLine(session, line);
            }

            if (Settings.CommandIgnore.Contains(messageSender.Id) || messageSender.Id == Steam.Bot.PersonaId)
                return;

            Command.Handle(CommandTarget.FromGroupChat(this, messageSender.Id), message, "~");
        }

        private void HandleEnter(Chat sender, Persona user)
        {
            var message = Steam.GetName(user.Id) + " entered chat.";

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), "Enter", Steam.GetName(user.Id), user.Id.ConvertToUInt64().ToString(), "", "0", message);
            AddHistory(line);

            foreach (var session in Program.Sessions.Values.ToList())
            {
                if (session.Chat == Name)
                    Program.SendHistoryLine(session, line);
            }
        }

        private void HandleLeave(Chat sender, Persona user, ChatLeaveReason reason, Persona sourceUser)
        {
            var message = Steam.GetName(user.Id);
            switch (reason)
            {
                case ChatLeaveReason.Left:
                    message += " left chat.";
                    break;
                case ChatLeaveReason.Disconnected:
                    message += " disconnected.";
                    break;
                case ChatLeaveReason.Kicked:
                    message += string.Format(" was kicked by {0}.", Steam.GetName(sourceUser.Id));
                    break;
                case ChatLeaveReason.Banned:
                    message += string.Format(" was banned by {0}.", Steam.GetName(sourceUser.Id));
                    break;
            }

            var by = sourceUser != null ? Steam.GetName(sourceUser.Id) : "";
            var byId = by != "" ? sourceUser.Id.ConvertToUInt64().ToString() : "0";

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), reason.ToString(), Steam.GetName(user.Id), user.Id.ConvertToUInt64().ToString(), by, byId, message);
            AddHistory(line);

            foreach (var session in Program.Sessions.Values.ToList())
            {
                if (session.Chat == Name)
                    Program.SendHistoryLine(session, line);
            }
        }

        private void AddHistory(HistoryLine line)
        {
            if (history.Count >= 75)
                history.RemoveFirst();
            history.AddLast(line);
        }
    }
}
