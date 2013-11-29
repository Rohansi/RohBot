using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SteamMobile.Packets;

namespace SteamMobile.Rooms
{
    public class RoomInfo
    {
        public readonly string Type;
        public readonly string Name;
        public readonly string ShortName;
        public readonly string Admin;

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
            Admin = properties["Admin"];

            _properties = properties;
        }
    }

    public class RoomSettings
    {
        public long Id { get; private set; }
        public string Room;
        public HashSet<string> Bans;
        public HashSet<string> Mods;

        public RoomSettings()
        {
            Id = 0;
        }

        internal RoomSettings(dynamic row)
        {
            Id = row.id;
            Room = row.room;
            Bans = new HashSet<string>(row.bans);
            Mods = new HashSet<string>(row.mods);
        }

        public void Save()
        {
            if (Id == 0)
                throw new InvalidOperationException("Cannot save row that does not exist");

            var cmd = new SqlCommand("UPDATE rohbot.roomsettings SET bans=:bans, mods=:mods WHERE id=:id;");
            cmd["id"] = Id;
            cmd["bans"] = Bans.ToArray();
            cmd["mods"] = Mods.ToArray();
            cmd.ExecuteNonQuery();
        }

        public void Insert()
        {
            if (Id != 0)
                throw new InvalidOperationException("Cannot insert existing row");

            var cmd = new SqlCommand("INSERT INTO rohbot.roomsettings (room,bans,mods) VALUES (:room,:bans,:mods) RETURNING id;");
            cmd["room"] = Room;
            cmd["bans"] = Bans.ToArray();
            cmd["mods"] = Mods.ToArray();
            Id = (long)cmd.ExecuteScalar();
        }

        public static RoomSettings Get(string room)
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.roomsettings WHERE lower(room)=lower(:room);");
            cmd["room"] = room;
            var row = cmd.Execute().FirstOrDefault();
            return row == null ? null : new RoomSettings(row);
        }
    }

    public class Room
    {
        public readonly RoomInfo RoomInfo;
        public bool IsActive { get; private set; }
        public readonly bool IsWhitelisted;
        public readonly bool IsHidden;

        /// <summary>
        /// If not empty, commands used in this room will potentially resolve to commands that use the prefix.
        /// </summary>
        public virtual string CommandPrefix { get { return ""; } }

        private RoomSettings _settings;
        private readonly LinkedList<HistoryLine> _history;
        private bool _showLinkTitles;

        public Room(RoomInfo roomInfo)
        {
            RoomInfo = roomInfo;
            IsActive = true;

            _settings = RoomSettings.Get(RoomInfo.ShortName);
            if (_settings == null)
            {
                _settings = new RoomSettings
                {
                    Room = RoomInfo.ShortName,
                    Bans = new HashSet<string>(),
                    Mods = new HashSet<string>()
                };
                _settings.Insert();
            }

            _history = new LinkedList<HistoryLine>();

            var cmd = new SqlCommand("SELECT * FROM rohbot.chathistory WHERE chat=lower(:chat) ORDER BY date DESC LIMIT 100;");
            cmd["chat"] = RoomInfo.ShortName;

            foreach (var line in cmd.Execute().Reverse().Select(r => HistoryLine.Read(r)))
            {
                _history.AddLast(line);
            }

            _showLinkTitles = (roomInfo["LinkTitles"] ?? "").ToLower() == "true";
            IsWhitelisted = (roomInfo["Whitelist"] ?? "").ToLower() == "true";
            IsHidden = (roomInfo["Hidden"] ?? "").ToLower() == "true";
        }

        /// <summary>
        /// Called when a message is being sent to the room. Should call base.
        /// </summary>
        public virtual void SendLine(HistoryLine line)
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

            var message = new Message();
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
            SendLine(line);
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
                var chatHistory = new ChatHistory { Name = RoomInfo.Name, ShortName = RoomInfo.ShortName, Requested = false, Lines = _history.ToList() };
                session.Send(chatHistory);
            }
        }

        /// <summary>
        /// Called when somebody sends a message. Probably not needed.
        /// </summary>
        public virtual void OnSendMessage(Session session, string message)
        {
            if (!message.StartsWith("//") && Command.Handle(new CommandTarget(session), message, "/"))
                return;

            if (!message.StartsWith("~~") && Command.Handle(new CommandTarget(session), message, "~"))
                return;

            if (message.StartsWith("//") || message.StartsWith("~~"))
                message = message.Substring(1);

            if (IsBanned(session.Account.Name))
            {
                session.Send(new SysMessage
                {
                    Date = Util.GetCurrentUnixTimestamp(),
                    Content = "You are banned from this room."
                });
                return;
            }

            var roomName = RoomInfo.ShortName;
            var userName = session.Account.Name;
            var userId = session.Account.Id.ToString();
            var userStyle = session.Account.EnabledStyle;
            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), roomName, "RohBot", userName, userId, userStyle, message, false);
            SendLine(line);
        }

        public virtual void Update()
        {

        }

        public List<string> Banned
        {
            get
            {
                lock (_settings)
                    return _settings.Bans.ToList();
            }
        }

        public virtual void Ban(string name)
        {
            lock (_settings)
            {
                _settings.Bans.Add(name.ToLower());
                _settings.Save();
            }
        }

        public void Unban(string name)
        {
            lock (_settings)
            {
                _settings.Bans.Remove(name.ToLower());
                _settings.Save();
            }
        }

        public bool IsBanned(string name)
        {
            lock (_settings)
            {
                return IsWhitelisted ^ _settings.Bans.Contains(name.ToLower());
            }
        }

        public List<string> Modded
        {
            get
            {
                lock (_settings)
                    return _settings.Mods.ToList();
            }
        }

        public void Mod(string name)
        {
            lock (_settings)
            {
                _settings.Mods.Add(name.ToLower());
                _settings.Save();
            }
        }

        public void Demod(string name)
        {
            lock (_settings)
            {
                _settings.Mods.Remove(name.ToLower());
                _settings.Save();
            }
        }

        public bool IsMod(string name)
        {
            name = name.ToLower();
            var banned = IsBanned(name);

            lock (_settings)
            {
                return (!banned && _settings.Mods.Contains(name.ToLower())) || name == RoomInfo.Admin.ToLower();
            }
        }

        private void AddHistory(HistoryLine line)
        {
            if (_history.Count >= 100)
                _history.RemoveFirst();
            _history.AddLast(line);

            line.Insert();
        }
    }
}
