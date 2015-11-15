using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RohBot.Rooms;

namespace RohBot
{
    public class RoomManager
    {
        private ConcurrentDictionary<string, Room> _rooms;

        public RoomManager()
        {
            _rooms = new ConcurrentDictionary<string, Room>();
        }

        public Room Get(string name)
        {
            Room result;
            _rooms.TryGetValue(name.ToLower(), out result);
            return result;
        }

        public ICollection<Room> List => _rooms.Values;

        public void Update()
        {
            lock (_rooms)
            {
                var deadRooms = _rooms.Where(kv => !kv.Value.IsActive);
                foreach (var dead in deadRooms)
                {
                    Room removedRoom;
                    _rooms.TryRemove(dead.Key, out removedRoom);
                }

                var settings = Program.Settings;

                try
                {
                    var oldRooms = _rooms.Where(r1 => settings.Rooms.All(r2 => r2["ShortName"] != r1.Value.RoomInfo.ShortName));
                    foreach (var room in oldRooms)
                    {
                        room.Value.Leave();
                    }
                }
                catch (Exception e)
                {
                    Program.Logger.Error("Failed to unload rooms", e);
                }

                try
                {
                    var newRooms = settings.Rooms.Where(r => !_rooms.ContainsKey(r["ShortName"]));
                    foreach (var room in newRooms)
                    {
                        var roomInfo = new RoomInfo(room);
                        var roomObj = (Room)Activator.CreateInstance(RoomTypes[roomInfo.Type], roomInfo);
                        _rooms.TryAdd(room["ShortName"], roomObj);
                    }
                }
                catch (Exception e)
                {
                    Program.Logger.Error("Failed to load rooms", e);
                }

                try
                {
                    foreach (var room in _rooms)
                    {
                        room.Value.Update();
                    }
                }
                catch (Exception e)
                {
                    Program.Logger.Error("Failed to update rooms", e);
                }
            }
        }

        public void Broadcast(string message, Func<Room, bool> filter = null)
        {
            foreach (var room in _rooms)
            {
                if (filter == null || filter(room.Value))
                {
                    room.Value.Send(message);
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
