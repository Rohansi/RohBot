using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace SteamMobile.Rooms
{
    public class RoomInfo
    {
        public readonly string Type;
        public readonly string Name;
        public readonly string ShortName;

        public string this[string key]
        {
            get
            {
                string value;
                _properties.TryGetValue(key, out value);
                return value;
            }
        }

        private IDictionary<string, string> _properties; 

        public RoomInfo(IDictionary<string, string> properties)
        {
            Type = properties["Type"];
            Name = properties["Name"];
            ShortName = properties["ShortName"];

            _properties = properties;
        }
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
        public bool IsActive { get; private set; }
        public readonly bool IsWhitelisted;

        /// <summary>
        /// If not null, commands used in this room will potentially resolve to commands that use the prefix.
        /// </summary>
        public virtual string CommandPrefix { get { return null; } }

        private RoomBans _bans;
        private readonly LinkedList<HistoryLine> _history;
        private bool _showLinkTitles;

        public Room(RoomInfo roomInfo)
        {
            RoomInfo = roomInfo;
            IsActive = true;

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

            _showLinkTitles = (roomInfo["LinkTitles"] ?? "").ToLower() == "true";
            IsWhitelisted = (roomInfo["Whitelist"] ?? "").ToLower() == "true";
        }

        /// <summary>
        /// Called when a message is beign sent to the room. Should call base.
        /// </summary>
        public virtual void Send(HistoryLine line)
        {
            var chatLine = line as ChatLine;
            if (chatLine != null && _showLinkTitles && chatLine.SenderId != "0")
            {
                ThreadPool.QueueUserWorkItem(a =>
                {
                    var titles = LinkTitles.Lookup(chatLine.Content);
                    if (!string.IsNullOrWhiteSpace(titles))
                        Send(titles);
                });
            }

            var message = new Packets.Message();
            message.Line = line;
            Program.SessionManager.Broadcast(message, s => s.Room == RoomInfo.ShortName);

            AddHistory(line);
        }

        /// <summary>
        /// Send a message as the bot. Should call base.
        /// </summary>
        public virtual void Send(string str)
        {
            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), RoomInfo.ShortName, "Steam", Program.Settings.PersonaName, "0", "", str, false);
            Send(line);
        }

        /// <summary>
        /// Called when leaving the room. Must call base.
        /// </summary>
        public virtual void Leave()
        {
            IsActive = false;
        }

        /// <summary>
        /// Called when somebody joins the room. Must call base.
        /// </summary>
        public virtual void SendHistory(Session session)
        {
            lock (_history)
            {
                var chatHistory = new Packets.ChatHistory { Name = RoomInfo.Name, ShortName = RoomInfo.ShortName, Requested = false, Lines = _history.ToList() };
                session.Send(chatHistory);
            }
        }

        public virtual void Update()
        {

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
                return IsWhitelisted ^ _bans.Bans.Contains(name.ToLower());
            }
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
