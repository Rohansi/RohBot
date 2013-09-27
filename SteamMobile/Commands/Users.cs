using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace SteamMobile.Commands
{
    public class Users : Command
    {
        public override string Type { get { return "users"; }  }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession)
                return;

            var roomName = target.Session.Room;
            var room = Program.RoomManager.Get(roomName);
            if (room == null)
            {
                target.Send("RohBot is not in the current chat.");
                return;
            }

            var userList = new Packets.UserList();
            var chat = room.Chat;
            var steamUsers = Program.Steam.Status == Steam.ConnectionStatus.Connected ? chat.Members.ToList() : new List<SteamID>();
            var sessions = Program.SessionManager.List.Where(s => s.Account != null).ToList();

            foreach (var id in steamUsers.Where(i => i != Program.Steam.Bot.PersonaId))
            {
                var persona = Program.Steam.Bot.GetPersona(id);
                var steamId = id.ConvertToUInt64().ToString("D");
                var groupMember = chat.Group.Members.FirstOrDefault(m => m.Id == id);
                var rank = groupMember != null ? groupMember.Rank.ToString() : "Member";
                var avatar = BitConverter.ToString(persona.Avatar).Replace("-", "").ToLower();
                userList.AddUser(persona.Name, steamId, rank, avatar, persona.PlayingName, false);
            }

            var accounts = sessions.Where(s => s.Room == roomName).Select(s => s.Account).Distinct(new Account.Comparer());
            
            foreach (var account in accounts)
            {
                userList.AddUser(account.Name, "0", "Member", "", "", true);
            }

            userList.Users = userList.Users.OrderBy(u => u.Name).ToList();
            target.Session.Send(userList);
        }
    }
}
