using System;
using System.Collections.Generic;
using System.Linq;
using RohBot.Packets;

namespace RohBot.Rooms
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
        public readonly bool IsPrivate;
        public readonly bool IsLogging;
        public readonly bool ShowWebStates;
        public readonly bool DisableBanning;

        /// <summary>
        /// If not empty, commands used in this room will potentially resolve to commands that use the prefix.
        /// </summary>
        public virtual string CommandPrefix => "";

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

            _showLinkTitles = (RoomInfo["LinkTitles"] ?? "").ToLower() == "true";
            IsWhitelisted = (RoomInfo["Whitelist"] ?? "").ToLower() == "true";
            IsHidden = (RoomInfo["Hidden"] ?? "").ToLower() == "true";
            IsPrivate = (RoomInfo["Private"] ?? "").ToLower() == "true";
            IsLogging = (RoomInfo["Logging"] ?? "true").ToLower() == "true";
            ShowWebStates = (RoomInfo["WebStates"] ?? "true").ToLower() == "true";
            DisableBanning = (RoomInfo["DisableBanning"] ?? "").ToLower() == "true";
        }

        /// <summary>
        /// Called when a message is being sent to the room. Should call base.
        /// </summary>
        public virtual void SendLine(HistoryLine line)
        {
            line = Util.EmoticonReplace(line);

            var chatLine = line as ChatLine;
            if (chatLine != null && _showLinkTitles && chatLine.SenderId != "0")
            {
                Action checkTitles = async () =>
                {
                    var titles = await LinkTitles.Lookup(chatLine.Content);
                    if (!string.IsNullOrWhiteSpace(titles))
                        Send(titles);
                };

                checkTitles();
            }

            var message = new Message();
            message.Line = line;

            Func<Session, bool> filter = session =>
            {
                if (session.Account == null)
                    return false;

                if (IsPrivate && IsBanned(session.Account.Name))
                    return false;

                return session.IsInRoom(RoomInfo.ShortName);
            };

            var sessions = Program.SessionManager.List.Where(filter);
            sessions = SendLineFilter(line, sessions);

            Program.SessionManager.Send(message, sessions);
            Program.NotificationManager.HandleMessage(this, message);

            AddHistory(line);
        }

        /// <summary>
        /// Filter sessions which receive lines from SendLine.
        /// </summary>
        public virtual IEnumerable<Session> SendLineFilter(HistoryLine line, IEnumerable<Session> sessions)
        {
            return sessions;
        }

        /// <summary>
        /// Send a message as the bot. Should call base.
        /// </summary>
        public virtual void Send(string str)
        {
            var line = new ChatLine(Util.GetCurrentTimestamp(), RoomInfo.ShortName, "Steam", Program.Settings.PersonaName, "0", "", str, false);
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
        /// Called when somebody joins the room. Should call base.
        /// </summary>
        public virtual void SendHistory(Connection connection)
        {
            if (IsPrivate)
            {
                if (connection.Session == null)
                {
                    ClearScrollbackFor(connection);
                    connection.SendSysMessage("You must login to view this room.");
                    return;
                }

                if (IsBanned(connection.Session.Account.Name))
                {
                    ClearScrollbackFor(connection);
                    connection.SendSysMessage("You are banned from this room.");
                    return;
                }
            }

            var lines = GetHistoryLines(connection);
            var chatHistory = new ChatHistory { ShortName = RoomInfo.ShortName, Requested = false, Lines = lines };
            connection.Send(chatHistory);
        }

        /// <summary>
        /// Called by SendHistory.
        /// </summary>
        public virtual List<HistoryLine> GetHistoryLines(Connection connection)
        {
            lock (_history)
                return _history.ToList();
        }

        /// <summary>
        /// Called when somebody sends a message.
        /// </summary>
        public virtual void SendMessage(Connection connection, string message)
        {
            if (connection.Session == null) // should never happen
                return;

            var roomName = RoomInfo.ShortName;
            var account = connection.Session.Account;

            if (IsBanned(account.Name))
            {
                connection.SendSysMessage("You are banned from this room.");
                return;
            }

            var userName = account.Name;
            var userId = account.Id.ToString("D");
            var userStyle = account.EnabledStyle;
            var line = new ChatLine(Util.GetCurrentTimestamp(), roomName, "RohBot", userName, userId, userStyle, message, false);
            SendLine(line);
        }

        // TODO: merge these similarly to the one in SteamRoom
        public void SessionEnter(Session session)
        {
            if (!ShowWebStates || session.Account == null || IsBanned(session.Account.Name))
                return;

            if (DateTime.Now - Program.StartTime <= TimeSpan.FromSeconds(20))
                return;

            var account = session.Account;
            var line = new StateLine(
                Util.GetCurrentTimestamp(),
                RoomInfo.ShortName,
                "Enter",
                account.Name, account.Id.ToString("D"), "RohBot", account.EnabledStyle,
                "", "0", "", "",
                account.Name + " entered chat.");

            SendLine(line);
        }

        public void SessionLeft(Session session)
        {
            if (!ShowWebStates || session.Account == null || IsBanned(session.Account.Name))
                return;

            var account = session.Account;
            var line = new StateLine(
                Util.GetCurrentTimestamp(),
                RoomInfo.ShortName,
                "Left",
                account.Name, account.Id.ToString("D"), "RohBot", account.EnabledStyle,
                "", "0", "", "",
                account.Name + " left chat.");

            SendLine(line);
        }

        public void SessionDisconnect(Session session)
        {
            if (!ShowWebStates || session.Account == null || IsBanned(session.Account.Name))
                return;

            var account = session.Account;
            var line = new StateLine(
                Util.GetCurrentTimestamp(),
                RoomInfo.ShortName,
                "Disconnected",
                account.Name, account.Id.ToString("D"), "RohBot", account.EnabledStyle,
                "", "0", "", "",
                account.Name + " disconnected.");

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
                if (!IsWhitelisted)
                    _settings.Bans.Add(name.ToLower());
                else
                    _settings.Bans.Remove(name.ToLower());

                _settings.Save();
            }
        }

        public void Unban(string name)
        {
            lock (_settings)
            {
                if (!IsWhitelisted)
                    _settings.Bans.Remove(name.ToLower());
                else
                    _settings.Bans.Add(name.ToLower());

                _settings.Save();
            }
        }

        public bool IsBanned(string name)
        {
            lock (_settings)
            {
                if (Util.IsAdmin(this, name))
                    return false;

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
            lock (_history)
            {
                if (_history.Count >= 100)
                    _history.RemoveFirst();
                _history.AddLast(line);
            }

            if (IsLogging)
                line.Insert();
        }

        private void ClearScrollbackFor(Connection connection)
        {
            var chatHistory = new ChatHistory { ShortName = RoomInfo.ShortName, Requested = false, Lines = new List<HistoryLine>() };
            connection.Send(chatHistory);
        }
    }
}
