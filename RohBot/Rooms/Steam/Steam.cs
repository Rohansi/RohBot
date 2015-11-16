using System;
using System.Diagnostics;
using System.Threading;
using EzSteam;
using SteamKit2;

namespace RohBot.Rooms.Steam
{
    public class Steam
    {
        public enum ConnectionStatus
        {
            Disconnected, Connected, Connecting
        }

        public ConnectionStatus Status { get; private set; }

        public SteamBot Bot => Status == ConnectionStatus.Connected ? _bot : null;

        private SteamBot _bot;
        private bool _hasConnected;
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
                _bot?.Disconnect();
                Status = ConnectionStatus.Disconnected;
            }

            if (Status != ConnectionStatus.Disconnected)
                return;

            if (string.IsNullOrWhiteSpace(Program.Settings.Username) ||
                string.IsNullOrWhiteSpace(Program.Settings.Password))
            {
                return;
            }

            _hasConnected = false;
            _connectStarted.Restart();
            Program.Logger.Info("Connecting");

            _bot = new SteamBot(Program.Settings.Username, Program.Settings.Password, Program.Settings.AuthCode);
            _bot.OnConnected += sender =>
            {
                _hasConnected = true;
                _connectStarted.Stop();

                _bot.DisplayName = Program.Settings.PersonaName;
                _bot.PersonaState = EPersonaState.Online;
                Status = ConnectionStatus.Connected;

                Program.Logger.Info("Connected");
            };

            _bot.OnDisconnected += (sender, reason) =>
            {
                if (reason == SteamBotDisconnectReason.SteamGuard)
                    Thread.Sleep(TimeSpan.FromMinutes(2)); // TODO: need a better way of entering steamguard auth

                if (_hasConnected)
                {
                    Program.Logger.InfoFormat("Disconnected {0}", reason);
                    _hasConnected = false;
                }

                Status = ConnectionStatus.Disconnected;
            };

            _bot.OnFriendRequest += (sender, user) => _bot.AddFriend(user.Id);

            _bot.OnPrivateEnter += (sender, chat) =>
            {
                chat.OnMessage += (chatSender, messageSender, message) =>
                    Command.Handle(new CommandTarget(chatSender, messageSender), message, "");
            };

            _bot.OnChatInvite += (sender, @by, chat) =>
            {
                if (chat.IsIndividualAccount)
                    _bot.Join(chat);
            };

            _bot.Connect();
            Status = ConnectionStatus.Connecting;
        }

        public void Disconnect()
        {
            _bot.Disconnect();
        }
    }
}
