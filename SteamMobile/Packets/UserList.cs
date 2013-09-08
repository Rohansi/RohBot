using System;
using System.Collections.Generic;

namespace SteamMobile.Packets
{
    public class UserList : Packet
    {
        public class User
        {
            public readonly string Name;
            public readonly string Rank;        // Owner/Officer/Moderator/Member/Guest
            public readonly string Avatar;      // Empty if only using web
            public readonly string Playing;     // Empty if not playing OR only using web
            public readonly bool Web;           // True if the account is logged in to the RohBot client

            public User(string name, string rank, string avatar, string playing, bool web)
            {
                Name = name;
                Rank = rank;
                Avatar = avatar;
                Playing = playing;
                Web = web;
            }
        }

        public override string Type { get { return "userList"; } }
        public List<User> Users = new List<User>();

        public void AddUser(string name, string rank, string avatar, string playing, bool web)
        {
            Users.Add(new User(name, rank, avatar, playing, web));
        }
    }
}
