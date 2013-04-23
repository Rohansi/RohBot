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
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Chat) || parameters.Length < 2)
                return;

            var receiver = parameters[0].ToLower();
            var message = parameters[1];

            var senderAccount = target.Account;
            var receiverAccount = Accounts.Find(receiver);

            if (receiverAccount == null)
            {
                target.Send("User does not exist.");
                return;
            }

            senderAccount.Reply = receiverAccount.Name;
            receiverAccount.Reply = senderAccount.Name;

            var sessions = Program.Sessions.Values.Where(s => s.Account == senderAccount || s.Account == receiverAccount).ToList();

            if (sessions.Count == 0)
            {
                target.Send("User does not exist or is offline.");
                return;
            }

            var line = new WhisperLine(Util.GetCurrentUnixTimestamp(), senderAccount.Name, receiverAccount.Name, message);
            Program.LogMessage(line);

            foreach (var session in sessions)
            {
                Program.SendHistoryLine(session, line);
            }
        }
    }
}
