using System;
using EzSteam;

namespace SteamMobile.Commands
{
    public class Rejoin : Command
    {
        public override string Type { get { return "rejoin"; } }

        public override string Format { get { return "-"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.Account.Permissions.HasFlag(Permissions.Ban) || parameters.Length < 1)
                return;

            GroupChat chat;
            if (!Program.Chats.TryGetValue(parameters[0], out chat))
            {
                target.Send("Chat not found.");
                return;
            }

            Program.Logger.InfoFormat("User '{0}' requesting rejoin for {1}.", target.Account.Name, parameters[0]);
            chat.Chat.Leave(ChatLeaveReason.Left);
        }
    }
}
