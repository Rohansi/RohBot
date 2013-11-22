using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamMobile.Rooms.Mafia;

namespace SteamMobile.Rooms
{
    public class MafiaRoom : Room
    {
        public override string CommandPrefix { get { return "mafia_"; } }

        private Dictionary<string, Player> _players;

        public bool IsPlaying { get; private set; }
        public bool IsDay { get; private set; }

        public List<Player> Players
        {
            get
            {
                lock (_players)
                    return _players.Values.ToList();
            }
        }

        public MafiaRoom(RoomInfo roomInfo)
            : base(roomInfo)
        {
            _players = new Dictionary<string, Player>();
        }
    }
}
