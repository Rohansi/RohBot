using System;
using System.Linq;
using SteamKit2;

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

            if (senderAccount == receiverAccount)
            {
                target.Send("You can not whisper yourself.");
                return;
            }

            senderAccount.Reply = receiverAccount.Name;
            receiverAccount.Reply = senderAccount.Name;

            var sessions = Program.Sessions.Values.Where(s => s.Account == senderAccount || s.Account == receiverAccount).ToList();

            if (sessions.Count == 0 && Steam.Bot.GetPersona(receiverAccount.Id).State == EPersonaState.Offline)
            {
                target.Send("User is offline.");
                return;
            }

            Steam.Bot.Join(receiverAccount.Id).Send(string.Format("From {0}: {1}", senderAccount.Name, message));

            var line = new WhisperLine(Util.GetCurrentUnixTimestamp(), message, senderAccount.Name, receiverAccount.Name);
            Program.AddWhisper(line);

            foreach (var session in sessions)
            {
                Program.SendHistoryLine(session, line);
            }
        }
    }
}
