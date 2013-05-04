using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamMobile.Commands
{
    public class Reply : Command
    {
        public override string Type { get { return "r"; } }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Chat))
                return;

            if (parameters.Length == 0)
            {
                var replyName = !string.IsNullOrWhiteSpace(target.Account.Reply) ? target.Account.Reply : null;
                target.Send(string.Format("Replying to {0}.", replyName ?? "nobody"));
                return;
            }

            var receiver = target.Account.Reply;
            var message = parameters[0];

            if (string.IsNullOrWhiteSpace(receiver))
            {
                target.Send("Nobody to reply to.");
                return;
            }

            var senderAccount = target.Account;
            var receiverAccount = Accounts.Find(receiver);

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

            var line = new WhisperLine(Util.GetCurrentUnixTimestamp(), senderAccount.Name, receiverAccount.Name, message);
            Program.AddWhisper(line);

            foreach (var session in sessions)
            {
                Program.SendHistoryLine(session, line);
            }
        }
    }
}
