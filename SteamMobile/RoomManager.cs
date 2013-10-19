using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SteamMobile.Rooms;

namespace SteamMobile
{
    public class RoomManager
    {
        private Dictionary<string, Room> _rooms;

        public RoomManager()
        {
            _rooms = new Dictionary<string, Room>();
        }

        public Room Get(string name)
        {
            lock (_rooms)
            {
                Room result;
                _rooms.TryGetValue(name.ToLower(), out result);
                return result;
            }
        }

        public List<string> Names
        {
            get
            {
                lock (_rooms)
                    return _rooms.Keys.ToList();
            }
        }

        public void Update()
        {
            lock (_rooms)
            {
                _rooms.RemoveAll(room => !room.Value.IsActive);

                var settings = Program.Settings;
                foreach (var room in _rooms.Values.Where(r1 => settings.Rooms.All(r2 => r2["ShortName"] != r1.RoomInfo.ShortName)).ToList())
                {
                    room.Leave();
                }

                foreach (var room in settings.Rooms.Where(r => !_rooms.ContainsKey(r["ShortName"])).ToList())
                {
                    var roomInfo = new RoomInfo(room);
                    var roomObj = (Room)Activator.CreateInstance(RoomTypes[roomInfo.Type], roomInfo);
                    _rooms.Add(room["ShortName"], roomObj);
                }

                foreach (var room in _rooms.Values)
                {
                    room.Update();
                }
            }
        }

        public void Broadcast(string message, Func<Room, bool> filter = null)
        {
            lock (_rooms)
            {
                foreach (var room in _rooms.Values)
                {
                    if (filter == null || filter(room))
                    {
                        room.Send(message);
                    }
                }
            }
        }

        #region Static
        private static readonly Dictionary<string, Type> RoomTypes;

        static RoomManager()
        {
            RoomTypes = new Dictionary<string, Type>();

            var assembly = Assembly.GetCallingAssembly();
            var types = assembly.GetExportedTypes().Where(type => typeof(Room).IsAssignableFrom(type));

            foreach (var type in types)
            {
                RoomTypes[type.Name] = type;
            }
        }
        #endregion
    }
}
