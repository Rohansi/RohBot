using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EzSteam;
using SteamKit2;
using log4net;

namespace SteamMobile
{
    public static class Steam
    {
        public enum ConnectionStatus
        {
            Disconnected, Connected, Connecting
        }

        public static ConnectionStatus Status { get; private set; }

        public static Bot Bot
        {
            get { return Status == ConnectionStatus.Connected ? bot : null; }
        }

        public static readonly ILog Logger = LogManager.GetLogger("Steam");
        private static string username, password;
        private static Bot bot;

        static Steam()
        {
            Status = ConnectionStatus.Disconnected;
            bot = null;
        }

        public static void Initialize(string user, string pass)
        {
            username = user;
            password = pass;
        }

        public static void Update()
        {
            if (Status != ConnectionStatus.Disconnected)
                return;

            bot = new Bot(username, password);
            bot.OnConnected += sender =>
            {
                bot.PersonaName = Settings.PersonaName;
                bot.PersonaState = EPersonaState.Online;
                Status = ConnectionStatus.Connected;
                Logger.Info("Connected");
            };

            bot.OnDisconnected += (sender, reason) =>
            {
                Status = ConnectionStatus.Disconnected;
                Logger.Info("Disconnected");
            };

            bot.OnFriendRequest += (sender, user) => bot.AddFriend(user.Id);

            bot.OnPrivateEnter += (sender, chat) =>
            {
                chat.OnMessage += (chatSender, messageSender, message) =>
                    Command.Handle(CommandTarget.FromPrivateChat(chatSender, messageSender.Id), message, "");
            };

            bot.OnChatInvite += (sender, chat, @by) =>
            {
                if (chat.Id.IsIndividualAccount)
                    bot.Join(chat.Id);
            };

            bot.Connect();
            Status = ConnectionStatus.Connecting;
        }

        public static string GetName(SteamID steamId)
        {
            var account = Accounts.Find(steamId);
            if (account != null)
                return account.Name;

            var id = steamId.ConvertToUInt64().ToString();
            string name;
            if (Settings.Alias.TryGetValue(id, out name))
                return name;

            return Bot.GetPersona(steamId).Name;
        }
    }
}
