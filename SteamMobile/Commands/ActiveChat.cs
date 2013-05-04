using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class ActiveChat : Command
    {
        public override string Type { get { return "chat"; } }

        public override string Format { get { return "--"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || !target.IsSession)
                return;

            if (parameters.Length < 1)
            {
                target.Send("Currently in: " + target.Session.Chat);
                return;
            }

            switch (parameters[0])
            {
                case "default":
                    var defaultSet = (parameters.Length < 2) ? Settings.DefaultChat : parameters[1];

                    if (!Program.Chats.ContainsKey(defaultSet.ToLower()))
                    {
                        target.Send("Chat does not exist.");
                        return;
                    }

                    target.Session.Chat = defaultSet;
                    target.Account.DefaultChat = defaultSet;
                    target.Account.Save();
                    Program.SendHistory(target.Session);
                    target.Send("Switched to chat: " + defaultSet);
                    break;
                case "list":
                    target.Send("Available chats: " + string.Join(", ", Program.Chats.Keys));
                    break;
                default:
                    if (!Program.Chats.ContainsKey(parameters[0].ToLower()))
                    {
                        target.Send("Chat does not exist.");
                        return;
                    }

                    target.Session.Chat = parameters[0];
                    Program.SendHistory(target.Session);
                    target.Send("Switched to chat: " + parameters[0]);
                    break;
            }
        }
    }
}
