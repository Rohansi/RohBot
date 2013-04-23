using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Users : Command
    {
        public override string Type { get { return "users"; }  }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession || Program.MainChat == null)
                return;

            var userList = new Packets.UserList();

            var group = Program.MainChat.Group;
            foreach (var id in Program.MainChat.Members.Where(i => i != Steam.Bot.PersonaId))
            {
                var groupMember = group.Members.FirstOrDefault(m => m.Id == id);
                userList.AddUser("Steam", groupMember != null ? groupMember.Rank.ToString() : "Member", Steam.GetName(id));
            }

            var sessions = Program.Sessions.Values.Select(ss => ss.Name).Distinct().ToList();
            foreach (var session in sessions)
            {
                userList.AddUser("RohBot", "Member", session);
            }

            userList.Users = userList.Users.OrderBy(u => u.Name).ToList();

            Program.Send(target.Session, userList);
        }
    }
}
