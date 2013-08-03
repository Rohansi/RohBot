using System;

namespace SteamMobile.Commands
{
    public class Me : Command
    {
        public override string Type { get { return "me"; }  }

        public override string Format { get { return "]"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Chat) || !target.IsSession || parameters.Length < 1)
                return;

            GroupChat chat;
            if (!Program.Chats.TryGetValue(target.Session.Chat, out chat))
            {
                Program.SendSysMessage(target.Session, "RohBot is not in the current chat.");
                return;
            }

            var msg = target.Account.Name + " " + parameters[0];
            chat.Send(msg);
        }
    }
}
