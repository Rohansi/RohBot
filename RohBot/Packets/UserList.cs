using System;
using System.Collections.Generic;

namespace RohBot.Packets
{
    // S -> C
    public class UserList : Packet
    {
        public class User
        {
            public readonly string Name;
            public readonly string UserId;
            public readonly string Rank;        // Owner/Officer/Moderator/Member/Guest
            public readonly string Avatar;      // Empty if using web
            public readonly string Status;      // Online/Busy/Away/Snooze/Looking to Trade/Looking to Play/Offline OR empty if using web
            public readonly string Playing;     // Null if not playing OR using web
            public readonly bool Web;           // True if the account is using the RohBot client

            internal User(string name, string userId, string rank, string avatar, string status, string playing, bool web)
            {
                Name = Util.HtmlEncode(name);
                UserId = userId;
                Rank = rank;
                Avatar = avatar;
                Status = status;
                Playing = playing;
                Web = web;
            }
        }

        public override string Type { get { return "userList"; } }
        public string ShortName;
        public List<User> Users = new List<User>();

        public void AddUser(string name, string userId, string rank, string avatar, string status, string playing, bool web)
        {
            Users.Add(new User(name, userId, rank, avatar, status, playing, web));
        }

        public override void Handle(Connection connection)
        {
            throw new NotSupportedException();
        }
    }
}
