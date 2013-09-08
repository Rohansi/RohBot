using System;
using System.Linq;

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
            var steamUsers = chat.Members.ToList();

            lock (Program.Sessions)
            {
                foreach (var id in steamUsers.Where(i => i != Steam.Bot.PersonaId))
                {
                    var persona = Steam.Bot.GetPersona(id);
                    var groupMember = chat.Group.Members.FirstOrDefault(m => m.Id == id);
                    var rank = groupMember != null ? groupMember.Rank.ToString() : "Member";
                    var avatar = BitConverter.ToString(persona.Avatar).Replace("-", "").ToLower();
                    var usingWeb = Program.Sessions.Values.Any(s => s.Chat == target.Session.Chat && s.Account != null && s.Account.Id == id);
                    userList.AddUser(Steam.GetName(id), rank, avatar, persona.PlayingName, usingWeb);
                }
            
                var accounts = Program.Sessions.Values.Where(s => s.Account != null && s.Chat == target.Session.Chat && steamUsers.All(id => s.Account.Id != id))
                                                      .Select(s => s.Account).Distinct();
                
                foreach (var account in accounts)
                {
                    userList.AddUser(account.Name, "Member", "", "", true);
                }
            }

            userList.Users = userList.Users.OrderBy(u => u.Name).ToList();
            Program.Send(target.Session, userList);
        }
    }
}
