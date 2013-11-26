using System.Linq;

namespace SteamMobile.Rooms.Mafia.Commands
{
    public class Users : Command
    {
        public override string Type { get { return "mafia_users"; } }

        public override string Format(CommandTarget target, string type) { return ""; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsSession || !target.IsRoom || !(target.Room is MafiaRoom))
                return;

            var room = (MafiaRoom)target.Room;
            var userList = new Packets.UserList();

            if (room.IsPlaying)
            {
                foreach (var player in room.Players)
                {
                    userList.AddUser(player.Alias, "0", "Member", "", "", true);
                }
            }
            else
            {
                var sessions = Program.SessionManager.List.Where(s => s.Account != null).ToList();
                var accounts = sessions.Where(s => s.Room == room.RoomInfo.ShortName).Select(s => s.Account).Distinct(new Account.Comparer());

                foreach (var account in accounts)
                {
                    var userId = account.Id.ToString();
                    userList.AddUser(account.Name, userId, "Member", "", "", true);
                }
            }

            userList.Users = userList.Users.OrderBy(u => u.Name).ToList();
            target.Session.Send(userList);
        }
    }
}
