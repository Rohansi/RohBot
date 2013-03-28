using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Whisper : Command
    {
        public override string Type { get { return "w"; }  }

        public override string Format { get { return "-]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession || parameters.Length < 2)
                return;

            parameters[0] = parameters[0].ToLower();
            var sessions = Program.Sessions.Values.Where(s => s.Name.ToLower() == parameters[0]).ToList();

            if (sessions.Count == 0)
            {
                Program.SendMessage(target.Session, "*", "User does not exist or is offline.");
                return;
            }

            foreach (var session in sessions)
            {
                Program.SendWhisper(session, target.Session.Name, session.Name, parameters[1]);
            }

            Program.SendWhisper(target.Session, target.Session.Name, sessions[0].Name, parameters[1]);
            Program.LogMessage(new WhisperLine(Util.GetCurrentUnixTimestamp(), target.Session.Name, sessions[0].Name, parameters[1]));
        }
    }
}
