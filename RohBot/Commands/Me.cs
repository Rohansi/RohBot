﻿
namespace RohBot.Commands
{
    public class Me : Command
    {
        public override string Type => "me";

        public override string Format(CommandTarget target, string type) => "]";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsWeb || !target.IsRoom || parameters.Length == 0)
                return;

            if (Program.DelayManager.AddAndCheck(target.Connection, DelayManager.Message))
                return;

            var username = target.Connection.Session.Account.Name;
            var room = target.Room;
            if (room.IsBanned(username))
            {
                target.Send("You are banned from this room.");
                return;
            }

            var line = new StateLine(
                Util.GetCurrentTimestamp(),
                target.Room.RoomInfo.ShortName,
                "Action",
                username,
                target.Connection.Session.Account.Id.ToString("D"),
                "RohBot",
                "", "0", "",
                $"{username} {parameters[0]}");

            target.Room.SendLine(line);
        }
    }
}
