using System;

namespace SteamMobile.Commands
{
    public class ActiveChat : Command
    {
        public override string Type { get { return "chat"; } }

        public override string Format { get { return "--"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (!target.IsSession)
                return;

            if (parameters.Length < 1)
            {
                target.Send("Currently talking in: " + target.Session.Chat);
                return;
            }

            switch (parameters[0])
            {
                case "default":
                    var defaultChat = target.Account == null ? Settings.DefaultChat : target.Account.DefaultChat;
                    var defaultSet = parameters.Length < 2 ? defaultChat : parameters[1];

                    if (!Program.Chats.ContainsKey(defaultSet.ToLower()))
                    {
                        target.Send("Chat does not exist.");
                        return;
                    }

                    target.Session.Chat = defaultSet;

                    if (target.Account != null)
                    {
                        target.Account.DefaultChat = defaultSet;
                        target.Account.Save();
                    }

                    Program.SendHistory(target.Session);
                    target.Send("Switched to chat: " + defaultSet);
                    break;

                case "list":
                    target.Send("Available chats: " + string.Join(", ", Program.Chats.Keys));
                    break;

                default:
                    var chatSet = parameters[0].ToLower();
                    if (!Program.Chats.ContainsKey(chatSet))
                    {
                        target.Send("Chat does not exist.");
                        return;
                    }

                    target.Session.Chat = chatSet;
                    Program.SendHistory(target.Session);
                    target.Send("Switched to chat: " + chatSet);
                    break;
            }
        }
    }
}
