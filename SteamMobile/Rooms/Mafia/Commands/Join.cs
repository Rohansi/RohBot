using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Rooms.Mafia.Commands
{
    public class Join : Command
    {
        public override string Type { get { return "mafia_join"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession || !target.IsRoom || target.Session.Account == null || !(target.Room is MafiaRoom))
                return;

            var room = (MafiaRoom)target.Room;

            if (room.IsPlaying)
            {
                target.Send("A game is currently in progress. You must wait until it has finished to join.");
                return;
            }

            if (parameters.Length < 1)
            {
                target.Send("You must specify an alias to join the game: /join Brian");
                return;
            }

            if (!Util.IsValidUsername(parameters[0]))
            {
                target.Send(Util.InvalidUsernameMessage);
                return;
            }

            // TODO: add to game
        }
    }
}
