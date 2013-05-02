using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Users : Command
    {
        public override string Type { get { return "users"; }  }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession || Program.MainChat == null)
                return;

            var userList = new Packets.UserList();

            var group = Program.MainChat.Group;
            foreach (var id in Program.MainChat.Members.Where(i => i != Steam.Bot.PersonaId))
            {
                var groupMember = group.Members.FirstOrDefault(m => m.Id == id);
                userList.AddUser("Steam", groupMember != null ? groupMember.Rank.ToString() : "Member", Steam.GetName(id), 1);
            }

            lock (Program.Sessions)
            {
                var accounts = Program.Sessions.Values.Select(s => s.Account).Distinct();
                foreach (var account in accounts.Where(account => account != null))
                {
                    userList.AddUser("RohBot", "Member", account.Name, Program.Sessions.Values.Count(s => s.Account == account));
                }
            }

            userList.Users = userList.Users.OrderBy(u => u.Name).ToList();

            if (parameters.Length > 0 && parameters[0] == "json")
                Program.Send(target.Session, userList);
            else
                target.Send("In this chat: " + string.Join(", ", userList.Users.Select(u => u.Name)));
        }
    }
}
