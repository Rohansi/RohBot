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
                    Chat.Leave(Chat.LeaveReason.Left);
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
                Chat.Leave(Chat.LeaveReason.Left);
                Chat = null;
            }

            Active = false;
        }

        private void HandleMessage(Chat sender, SteamID messageSender, string message)
        {
            var senderName = Steam.GetName(messageSender);
            var senderType = "Steam";

            if (messageSender == Steam.Bot.PersonaId && message.StartsWith("["))
            {
                senderType = "RohBot";
                var nameEnd = message.IndexOf(']');
                senderName = message.Substring(1, nameEnd - 1);
                message = message.Substring(nameEnd + 2);
            }

            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), senderType, senderName, message);
            AddHistory(line);

            foreach (var session in Program.Sessions.Values.ToList())
            {
                if (session.Chat == Name)
                    Program.SendHistoryLine(session, line);
            }

            if (Settings.CommandIgnore.Contains(messageSender) || messageSender == Steam.Bot.PersonaId)
                return;

            Command.Handle(CommandTarget.FromGroupChat(this, messageSender), message, "~");
        }

        private void HandleEnter(Chat sender, SteamID user)
        {
            var message = Steam.GetName(user) + " entered chat.";

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), "Enter", Steam.GetName(user), "", message);
            AddHistory(line);

            foreach (var session in Program.Sessions.Values.ToList())
            {
                if (session.Chat == Name)
                    Program.SendHistoryLine(session, line);
            }
        }

        private void HandleLeave(Chat sender, SteamID user, Chat.LeaveReason reason, SteamID sourceUser)
        {
            var message = Steam.GetName(user);
            switch (reason)
            {
                case Chat.LeaveReason.Left:
                    message += " left chat.";
                    break;
                case Chat.LeaveReason.Disconnected:
                    message += " disconnected.";
                    break;
                case Chat.LeaveReason.Kicked:
                    message += string.Format(" was kicked by {0}.", Steam.GetName(sourceUser));
                    break;
                case Chat.LeaveReason.Banned:
                    message += string.Format(" was banned by {0}.", Steam.GetName(sourceUser));
                    break;
            }

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), reason.ToString(), Steam.GetName(user), sourceUser != null ? Steam.GetName(sourceUser) : "", message);
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
