using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Packets
{
    public class UserList : Packet
    {
        public class User
        {
            public readonly string UserType;    // RohBot/Steam
            public readonly string Rank;        // Owner/Officer/Moderator/Member/Guest
            public readonly string Name;
            public readonly string Avatar;      // Only valid for Steam users

            public User(string userType, string rank, string name, string avatar)
            {
                UserType = userType;
                Rank = rank;
                Name = name;
                Avatar = avatar;
            }
        }

        public override string Type { get { return "userList"; } }
        public List<User> Users = new List<User>();

        public void AddUser(string type, string rank, string name, string avatar)
        {
            Users.Add(new User(type, rank, name, avatar));
        }
    }
}
