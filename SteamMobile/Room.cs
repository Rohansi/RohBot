using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using EzSteam;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace SteamMobile
{
    public class RoomInfo
    {
        public string Name;
        public string ShortName;
        public string Link;
        public string Id;
    }

    public class RoomBans
    {
        public ObjectId Id;
        public string Room;
        public HashSet<string> Bans;
    }

    public class Room
    {
        public readonly RoomInfo RoomInfo;
        public bool Active { get; private set; }
        public Chat Chat { get; private set; }

        private RoomBans _bans;
        private readonly LinkedList<HistoryLine> _history;

        public Room(RoomInfo roomInfo)
        {
            RoomInfo = roomInfo;
            Active = true;

            _bans = Database.RoomBans.AsQueryable().FirstOrDefault(r => r.Room == RoomInfo.ShortName);
            if (_bans == null)
            {
                _bans = new RoomBans
                {
                    Room = RoomInfo.ShortName,
                    Bans = new HashSet<string>()
                };
                Database.RoomBans.Insert(_bans);
            }

            _history = new LinkedList<HistoryLine>();
            var lines = Database.ChatHistory.AsQueryable().Where(r => r.Chat == RoomInfo.ShortName).OrderByDescending(r => r.Date).Take(100).ToList();
            lines.Reverse();
            foreach (var line in lines)
            {
                _history.AddLast(line);
            }
        }

        public void Send(HistoryLine line)
        {
            var chatLine = line as ChatLine;
            if (chatLine != null)
            {
                if (chatLine.SenderId != "0")
                {
                    ThreadPool.QueueUserWorkItem(a =>
                    {
                        var titles = LinkTitles.Lookup(chatLine.Content);
                        if (!string.IsNullOrWhiteSpace(titles))
                            Send(titles);
                    });
                }

                if (Chat != null && chatLine.UserType == "RohBot")
                {
                    Chat.Send(string.Format("[{0}] {1}", WebUtility.HtmlDecode(chatLine.Sender), WebUtility.HtmlDecode(chatLine.Content)));
                }
            }

            AddHistory(line);

            var message = new Packets.Message();
            message.Line = line;
            Program.SessionManager.Broadcast(message, s => s.Room == RoomInfo.ShortName);
        }

        public void Send(string str)
        {
            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), RoomInfo.ShortName, "Steam", Program.Settings.PersonaName, "0", str, false);
            Send(line);
            Chat.Send(str);
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

        public void SendHistory(Session session)
        {
            lock (_history)
            {
                var chatHistory = new Packets.ChatHistory { Requested = false, Chat = RoomInfo.ShortName, Lines = _history.ToList() };
                session.Send(chatHistory);
            }
        }

        public List<string> Banned
        {
            get
            {
                lock (_bans)
                    return _bans.Bans.ToList();
            }
        } 

        public void Ban(string name)
        {
            lock (_bans)
            {
                _bans.Bans.Add(name.ToLower());
                Database.RoomBans.Save(_bans);
            }
        }

        public void Unban(string name)
        {
            lock (_bans)
            {
                _bans.Bans.Remove(name.ToLower());
                Database.RoomBans.Save(_bans);
            }
        }

        public bool IsBanned(string name)
        {
            lock (_bans)
            {
                return _bans.Bans.Contains(name.ToLower());
            }
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

            if (Program.Steam.Status != Steam.ConnectionStatus.Connected || Chat != null)
                return;

            Chat = Program.Steam.Bot.Join(ulong.Parse(RoomInfo.Id));
            Chat.OnEnter += sender => Program.Logger.Info("Entered " + RoomInfo.ShortName);
            Chat.OnLeave += (sender, reason) =>
            {
                Program.Logger.Info("Left " + RoomInfo.ShortName + ": " + reason);
                Chat = null;
            };

            Chat.OnMessage += HandleMessage;
            Chat.OnUserEnter += HandleEnter;
            Chat.OnUserLeave += HandleLeave;
        }

        private void HandleMessage(Chat sender, Persona messageSender, string message)
        {
            var senderName = messageSender.Name;
            var senderId = messageSender.Id.ConvertToUInt64().ToString("D");
            var inGame = messageSender.Playing != null && messageSender.Playing.ToUInt64() != 0;

            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), RoomInfo.ShortName, "Steam", senderName, senderId, message, inGame);
            Send(line);

            Command.Handle(new CommandTarget(this, messageSender.Id), message, "~");
        }

        private void HandleEnter(Chat sender, Persona user)
        {
            var message = user.Name + " entered chat.";

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), RoomInfo.ShortName, "Enter", user.Name, user.Id.ConvertToUInt64().ToString("D"), "", "0", message);
            Send(line);
        }

        private void HandleLeave(Chat sender, Persona user, ChatLeaveReason reason, Persona sourceUser)
        {
            var message = user.Name;
            switch (reason)
            {
                case ChatLeaveReason.Left:
                    message += " left chat.";
                    break;
                case ChatLeaveReason.Disconnected:
                    message += " disconnected.";
                    break;
                case ChatLeaveReason.Kicked:
                    message += string.Format(" was kicked by {0}.", sourceUser.Name);
                    break;
                case ChatLeaveReason.Banned:
                    message += string.Format(" was banned by {0}.", sourceUser.Name);
                    break;
            }

            var by = sourceUser != null ? sourceUser.Name : "";
            var byId = sourceUser != null ? sourceUser.Id.ConvertToUInt64().ToString("D") : "0";

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), RoomInfo.ShortName, reason.ToString(), user.Name, user.Id.ConvertToUInt64().ToString("D"), by, byId, message);
            Send(line);
        }

        private void AddHistory(HistoryLine line)
        {
            if (_history.Count >= 100)
                _history.RemoveFirst();
            _history.AddLast(line);

            Database.ChatHistory.Insert(line);
        }
    }
}
