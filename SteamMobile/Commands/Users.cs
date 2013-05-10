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
            if (!target.IsSession)
                return;

            GroupChat groupChat;
            if (!Program.Chats.TryGetValue(target.Session.Chat, out groupChat))
            {
                Program.SendSysMessage(target.Session, "RohBot is not in the current chat.");
                return;
            }

            var userList = new Packets.UserList();

            var chat = groupChat.Chat;
            foreach (var id in chat.Members.Where(i => i != Steam.Bot.PersonaId))
            {
                var groupMember = chat.Group.Members.FirstOrDefault(m => m.Id == id);
                var rank = groupMember != null ? groupMember.Rank.ToString() : "Member";
                var avatar = BitConverter.ToString(Steam.Bot.GetPersona(id).Avatar).Replace("-", "").ToLower();
                userList.AddUser("Steam", rank, Steam.GetName(id), avatar);
            }

            lock (Program.Sessions)
            {
                var accounts = Program.Sessions.Values.Where(s => s.Chat == target.Session.Chat)
                                                      .Select(s => s.Account).Distinct();
                
                foreach (var account in accounts.Where(account => account != null))
                {
                    userList.AddUser("RohBot", "Member", account.Name, "");
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
