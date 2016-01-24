using System;
using System.Linq;
using RohBot.Rooms.Steam;
using SteamKit2;
using RohBot.Rooms;

namespace RohBot.Commands
{
    public class Users : Command
    {
        public override string Type => "users";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || target.IsPrivateChat)
                return;

            var room = target.Room;
            var roomName = room.RoomInfo.ShortName;

            var sessions = Program.SessionManager.List;
            var accounts = sessions.Where(s => s.IsInRoom(roomName))
                                   .Select(s => s.Account)
                                   .Distinct(new Account.Comparer());

            if (target.IsWeb)
            {
                var userList = new Packets.UserList();

                var steamRoom = room as SteamRoom;
                var chat = steamRoom != null && Program.Steam.Status == Steam.ConnectionStatus.Connected ? steamRoom.Chat : null;

                if (chat != null)
                {
                    var steamUsers = chat.Users.Select(p => p.Id)
                                               .Where(i => i != Program.Steam.Bot.SteamId);

                    foreach (var id in steamUsers)
                    {
                        var persona = Program.Steam.Bot.GetPersona(id);
                        var steamId = id.ConvertToUInt64().ToString("D");
                        var groupMember = chat.Group.Members.FirstOrDefault(m => m.Persona.Id == id);
                        var rank = groupMember != null ? GetRankString(groupMember.Rank) : "Guest";
                        var avatar = BitConverter.ToString(persona.Avatar).Replace("-", "").ToLower();
                        var status = GetStatusString(persona.State);
                        userList.AddUser(persona.DisplayName, steamId, rank, avatar, status, persona.PlayingName, false, "");
                    }
                }

                foreach (var account in accounts)
                {
                    var userId = account.Id.ToString("D");
                    var rank = GetRankString(target.Room, account.Name);
                    userList.AddUser(account.Name, userId, rank, "", "", null, true, account.EnabledStyle);
                }

                userList.ShortName = roomName;
                userList.Users = userList.Users.OrderBy(u => u.Name, StringComparer.InvariantCultureIgnoreCase).ToList();
                target.Connection.Send(userList);
            }
            else
            {
                var names = accounts.OrderBy(a => a.Name, StringComparer.InvariantCultureIgnoreCase)
                                    .Select(a => a.Name);

                target.Send("In this room: " + string.Join(", ", names));
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

        private static string GetRankString(EClanPermission permission)
        {
            switch (permission)
            {
                case EClanPermission.Owner:
                    return "Administrator";
                case EClanPermission.Officer:
                case EClanPermission.Moderator:
                    return "Moderator";
                case EClanPermission.Member:
                    return "Member";
                default:
                    return "Guest";
            }
        }

        private static string GetRankString(Room room, string username)
        {
            if (Util.IsAdmin(room, username))
                return "Administrator";
            if (Util.IsMod(room, username))
                return "Moderator";
            if (room.IsBanned(username))
                return "Guest";
            return "Member";
        }
    }
}
