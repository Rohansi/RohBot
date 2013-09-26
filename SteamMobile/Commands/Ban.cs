using System;
using System.Collections.Generic;
using System.Linq;
using EzSteam;
using SteamKit2;

namespace SteamMobile.Commands
{
    public class Ban : Command
    {
        public override string Type { get { return "ban"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsGroupChat || parameters.Length == 0)
                return;

            var member = target.Room.Chat.Group.Members.FirstOrDefault(m => m.Id == target.SteamId);
            if (member == null || (member.Rank != ClanRank.Owner && member.Rank != ClanRank.Officer && member.Rank != ClanRank.Moderator))
                return;

            parameters[0] = parameters[0].ToLower();
            var inRoom = Program.SessionManager.List.Where(s => s.Room == target.Room.RoomInfo.ShortName && s.AccountInfo.Name.ToLower() == parameters[0]).ToList();
            if (inRoom.Count > 0)
            {
                target.Room.Ban(ulong.Parse(inRoom[0].AccountInfo.SteamId));
                target.Send("Account banned.");
                return;
            }

            ulong steamId;
            if (ulong.TryParse(parameters[0], out steamId) && ((SteamID)steamId).IsIndividualAccount)
            {
                target.Room.Ban(steamId);
                target.Send("Account banned.");
                return;
            }

            target.Send("No matching name or SteamID.");
        }
    }
}
