using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;
using SteamMobile.Rooms;

namespace SteamMobile.Commands
{
    public class Users : Command
    {
        public override string Type { get { return "users"; } }

        public override string Format(CommandTarget target, string type) { return "]"; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom && !target.IsPrivateChat)
            {
                target.Send("RohBot is not in this room.");
                return;
            }

            var room = target.Room;
            var roomName = room.RoomInfo.ShortName;

            var sessions = Program.SessionManager.List;
            var accounts = sessions.Where(s => s.IsInRoom(roomName))
                                   .Select(s => s.Account)
                                   .Distinct(new Account.Comparer());

            if (target.IsWeb)
            {
                // TODO: clean up this garbage
                var steamRoom = room as SteamRoom;
                var userList = new Packets.UserList();
                var chat = Program.Steam.Status == Steam.ConnectionStatus.Connected && steamRoom != null ? steamRoom.Chat : null;
                var steamUsers = chat != null ? chat.Members.ToList() : new List<SteamID>();

                foreach (var id in steamUsers.Where(i => i != Program.Steam.Bot.PersonaId))
                {
                    var persona = Program.Steam.Bot.GetPersona(id);
                    var steamId = id.ConvertToUInt64().ToString("D");
                    var groupMember = chat.Group.Members.FirstOrDefault(m => m.Id == id);
                    var rank = groupMember != null ? groupMember.Rank.ToString() : "Member";
                    var avatar = BitConverter.ToString(persona.Avatar).Replace("-", "").ToLower();
                    var status = GetStatusString(persona.State);
                    userList.AddUser(persona.Name, steamId, rank, avatar, status, persona.PlayingName, false);
                }

                foreach (var account in accounts)
                {
                    var userId = account.Id.ToString();
                    var rank = Util.GetRank(target.Room, account.Name).ToString();
                    userList.AddUser(account.Name, userId, rank, "", "", "", true);
                }

                userList.Users = userList.Users.OrderBy(u => u.Name).ToList();
                target.Connection.Send(userList);
            }
            else
            {
                target.Send("In this room: " + string.Join(", ", accounts.Select(a => a.Name)));
            }
        }

        private static string GetStatusString(EPersonaState status)
        {
            switch (status)
            {
                case EPersonaState.Offline:
                    return "Offline";
                case EPersonaState.Online:
                    return "Online";
                case EPersonaState.Busy:
                    return "Busy";
                case EPersonaState.Away:
                    return "Away";
                case EPersonaState.Snooze:
                    return "Snooze";
                case EPersonaState.LookingToTrade:
                    return "Looking to Trade";
                case EPersonaState.LookingToPlay:
                    return "Looking to Play";
                default:
                    return "";
            }
        }
    }
}
