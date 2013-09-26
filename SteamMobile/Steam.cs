using System;
using System.Diagnostics;
using EzSteam;
using SteamKit2;

namespace SteamMobile
{
    public class Steam
    {
        public enum ConnectionStatus
        {
            Disconnected, Connected, Connecting
        }

        public ConnectionStatus Status { get; private set; }

        public Bot Bot
        {
            get { return Status == ConnectionStatus.Connected ? _bot : null; }
        }

        private Bot _bot;
        private Stopwatch _connectStarted = Stopwatch.StartNew();

        public Steam()
        {
            Status = ConnectionStatus.Disconnected;
            _bot = null;
        }

        public void Update()
        {
            if (_connectStarted.Elapsed.TotalSeconds > 120)
            {
                if (_bot != null)
                    _bot.Disconnect();
                Status = ConnectionStatus.Disconnected;
            }

            if (Status != ConnectionStatus.Disconnected)
                return;

            _connectStarted.Restart();
            Program.Logger.Info("Connecting");

            _bot = new Bot(Program.Settings.Username, Program.Settings.Password);
            _bot.OnConnected += sender =>
            {
                _connectStarted.Stop();

                _bot.PersonaName = Program.Settings.PersonaName;
                _bot.PersonaState = EPersonaState.Online;
                Status = ConnectionStatus.Connected;

                Program.Logger.Info("Connected");
            };

            _bot.OnDisconnected += (sender, reason) =>
            {
                Status = ConnectionStatus.Disconnected;
                Program.Logger.Info("Disconnected");
            };

            _bot.OnFriendRequest += (sender, user) => _bot.AddFriend(user.Id);

            _bot.OnPrivateEnter += (sender, chat) =>
            {
                chat.OnMessage += (chatSender, messageSender, message) =>
                    Command.Handle(new CommandTarget(chatSender, messageSender.Id), message, "");
            };

            _bot.OnChatInvite += (sender, chat, @by) =>
            {
                if (chat.Id.IsIndividualAccount)
                    _bot.Join(chat.Id);
            };

            _bot.Connect();
            Status = ConnectionStatus.Connecting;
        }
    }
}
