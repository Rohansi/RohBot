using System.Collections.Generic;
using System.Linq;

namespace SteamMobile
{
    public class Session
    {
        public Account Account { get; private set; }
        public float TimeWithoutConnections { get; private set; }

        private List<Connection> _connections;
        private HashSet<string> _rooms;

        public Session(Account account)
        {
            Account = account;

            _connections = new List<Connection>();
            _rooms = new HashSet<string>(Account.Rooms);

            if (_rooms.Count == 0)
                _rooms.Add(Program.Settings.DefaultRoom);
        }

        public bool IsInRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                roomName = Program.Settings.DefaultRoom;

            roomName = roomName.ToLower();

            lock (_rooms)
            {
                return _rooms.Contains(roomName);
            }
        }

        public void Add(Connection connection)
        {
            lock (_connections)
            {
                if (!_connections.Contains(connection))
                    _connections.Add(connection);

                connection.Session = this;

                lock (_rooms)
                {
                    foreach (var roomName in _rooms)
                    {
                        var room = Program.RoomManager.Get(roomName);
                        connection.SendJoinRoom(room);
                    }
                }
            }
        }

        public void Update(float delta)
        {
            lock (_connections)
            {
                _connections.RemoveAll(conn => !conn.Connected);
            }

            TimeWithoutConnections += delta;
            if (_connections.Count > 0)
                TimeWithoutConnections = 0;
        }

        public bool Join(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                roomName = Program.Settings.DefaultRoom;

            roomName = roomName.ToLower();

            lock (_rooms)
            {
                if (_rooms.Contains(roomName))
                    return true;

                var room = Program.RoomManager.Get(roomName);
                if (room == null)
                    return false;

                _rooms.Add(room.RoomInfo.ShortName);

                lock (_connections)
                {
                    foreach (var conn in _connections)
                    {
                        conn.SendJoinRoom(room);
                    }
                }

                Account.Rooms = _rooms.ToArray();
                Account.Save();

                // TODO: can provide enter message

                return true;
            }
        }

        public bool Leave(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                roomName = Program.Settings.DefaultRoom;

            roomName = roomName.ToLower();

            bool joinDefault;

            lock (_rooms)
            {
                if (!_rooms.Contains(roomName))
                    return true;

                _rooms.Remove(roomName);
                joinDefault = _rooms.Count == 0;

                var room = Program.RoomManager.Get(roomName);

                lock (_connections)
                {
                    foreach (var conn in _connections)
                    {
                        conn.SendLeaveRoom(room);
                    }
                }

                Account.Rooms = _rooms.ToArray();
                Account.Save();

                // TODO: can provide leave message
            }

            if (joinDefault)
                Join(Program.Settings.DefaultRoom);

            return true;
        }

        public void Send(Packet packet)
        {
            var packetStr = Packet.WriteToMessage(packet);
            Send(packetStr);
        }

        public void Send(string data)
        {
            foreach (var conn in _connections)
            {
                conn.Send(data);
            }
        }
    }
}
