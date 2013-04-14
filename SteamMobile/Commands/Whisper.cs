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
            if (target.Account == null || parameters.Length < 2)
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

            foreach (var session in Program.Sessions.Values)
            {
                if (session.Account == senderAccount)
                    Program.SendWhisper(session, senderAccount.Name, receiverAccount.Name, message);
                if (session.Account == receiverAccount)
                    Program.SendWhisper(session, receiverAccount.Name, senderAccount.Name, message);
            }

            Program.LogMessage(new WhisperLine(Util.GetCurrentUnixTimestamp(), senderAccount.Name, receiverAccount.Name, message));
        }
    }
}
