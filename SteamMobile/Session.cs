using System.Collections.Generic;
using System.Linq;

namespace SteamMobile
{
    public class Session
    {
        public Account Account { get; private set; }
        public float TimeWithoutConnections { get; private set; }

        private readonly object _sync = new object();
        private List<Connection> _connections;
        private OrderedSet<string> _rooms;

        public Session(Account account)
        {
            Account = account;

            _connections = new List<Connection>();

            var defaultRoom = Program.Settings.DefaultRoom;
            var roomsList = new List<string>(Account.Rooms ?? new string[0]);
            var defaultIdx = roomsList.IndexOf(defaultRoom);
            if (roomsList.Count == 0)
            {
                roomsList.Add(defaultRoom);
            }
            else if (defaultIdx == -1)
            {
                roomsList.Insert(0, defaultRoom);
            }
            else if (defaultIdx > 0)
            {
                roomsList.RemoveAt(defaultIdx);
                roomsList.Insert(0, defaultRoom);
            }

            if (!roomsList.SequenceEqual(Account.Rooms ?? new string[0]))
            {
                Account.Rooms = roomsList.ToArray();
                Account.Save();
            }

            _rooms = new OrderedSet<string>(Account.Rooms);
        }

        public bool IsInRoom(string roomName)
        {
            lock (_sync)
            {
                roomName = (roomName ?? "").ToLower();
                return _rooms.Contains(roomName);
            }
        }

        public void Add(Connection connection)
        {
            lock (_sync)
            {
                if (_connections.Contains(connection))
                    return;

                _connections.Add(connection);

                connection.Session = this;

                foreach (var roomName in _rooms)
                {
                    var room = Program.RoomManager.Get(roomName);
                    connection.SendJoinRoom(room);
                }
            }
        }

        public void Remove(Connection connection)
        {
            lock (_sync)
            {
                _connections.Remove(connection);
            }
        }

        public void Update(float delta)
        {
            lock (_sync)
            {
                _connections.RemoveAll(conn => !conn.Connected);
            }

            TimeWithoutConnections += delta;
            if (_connections.Count > 0)
                TimeWithoutConnections = 0;
        }

        public bool Join(string roomName)
        {
            roomName = (roomName ?? "").ToLower();

            if (_rooms.Contains(roomName))
                return true;

            var room = Program.RoomManager.Get(roomName);
            if (room == null)
                return false;

            lock (_sync)
            {
                _rooms.Add(room.RoomInfo.ShortName);

                foreach (var conn in _connections)
                {
                    conn.SendJoinRoom(room);
                }

                Account.Rooms = _rooms.ToArray();
                Account.Save();
            }

            // TODO: can provide enter message

            return true;
        }

        public bool Leave(string roomName)
        {
            roomName = (roomName ?? "").ToLower();

            if (roomName == Program.Settings.DefaultRoom)
                return true;

            lock (_sync)
            {
                if (!_rooms.Contains(roomName))
                    return true;

                _rooms.Remove(roomName);

                var room = Program.RoomManager.Get(roomName);
            
                foreach (var conn in _connections)
                {
                    conn.SendLeaveRoom(room);
                }
            
                Account.Rooms = _rooms.ToArray();
                Account.Save();
            }

            // TODO: can provide leave message

            return true;
        }

        public void Send(Packet packet)
        {
            var packetStr = Packet.WriteToMessage(packet);
            Send(packetStr);
        }

        public void Send(string data)
        {
            lock (_sync)
            {
                foreach (var conn in _connections)
                {
                    conn.Send(data);
                }
            }
        }
    }
}
