using System.Collections.Generic;
using System.Linq;

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
                _rooms.RemoveAll(room => !room.Value.Active);

                var settings = Program.Settings;
                foreach (var room in _rooms.Values.Where(r1 => settings.Rooms.All(r2 => r2.ShortName != r1.RoomInfo.ShortName)).ToList())
                {
                    room.Leave();
                }
                    
                foreach (var room in settings.Rooms.Where(r => !_rooms.ContainsKey(r.ShortName)).ToList())
                {
                    _rooms.Add(room.ShortName, new Room(room));
                }

                foreach (var room in _rooms.Values)
                {
                    room.Update();
                }
            }
        }
    }
}
