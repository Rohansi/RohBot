using System.Linq;
using EzSteam;

namespace SteamMobile.Commands
{
    public class Unban : Command
    {
        public override string Type { get { return "unban"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsGroupChat || parameters.Length == 0)
                return;

            var member = target.Room.Chat.Group.Members.FirstOrDefault(m => m.Id == target.SteamId);
            if (member == null || (member.Rank != ClanRank.Owner && member.Rank != ClanRank.Officer && member.Rank != ClanRank.Moderator))
                return;

            target.Room.Unban(parameters[0]);
            target.Send("Account unbanned.");
        }
    }
}
