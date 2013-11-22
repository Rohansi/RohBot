using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Rooms.Mafia
{
    public class Player
    {
        /// <summary>
        /// RohBot Username
        /// </summary>
        public readonly string Username;

        /// <summary>
        /// Alias username specified by join command
        /// </summary>
        public readonly string Alias;
        
        public Player(string username, string alias)
        {
            Username = username;
            Alias = alias;
        }
    }
}
