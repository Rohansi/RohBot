using System;
using System.Diagnostics;
using System.Net;
using EzSteam;
using SteamKit2;

namespace RohBot.Rooms.Steam
{
    public class SteamRoom : Room
    {
        public SteamChat Chat { get; private set; }

        public readonly SteamID SteamId;
        public readonly bool EchoWebStates;

        private bool _hasConnected;
        private Stopwatch _lastMessage;

        public SteamRoom(RoomInfo roomInfo)
            : base(roomInfo)
        {
            _lastMessage = Stopwatch.StartNew();

            SteamId = new SteamID(ulong.Parse(RoomInfo["SteamId"]));
            EchoWebStates = (RoomInfo["EchoWebStates"] ?? "true").ToLower() == "true";
        }

        public override void SendLine(HistoryLine line)
        {
            var chatLine = line as ChatLine;
            if (chatLine != null && Chat != null && chatLine.UserType == "RohBot")
            {
                Chat.Send($"[{WebUtility.HtmlDecode(chatLine.Sender)}] {WebUtility.HtmlDecode(chatLine.Content)}");
            }

            var stateLine = line as StateLine;
            if (EchoWebStates && stateLine != null && Chat != null && stateLine.ForType == "RohBot" && stateLine.State != "Action")
            {
                Chat.Send("> " + WebUtility.HtmlDecode(stateLine.Content));
            }

            if (stateLine != null && Chat != null && stateLine.State == "Action")
            {
                Chat.Send(WebUtility.HtmlDecode(stateLine.Content));
            }

            base.SendLine(line);
        }

        public override void Send(string str)
        {
            Chat?.Send(str);

            base.Send(str);
        }

        public void Disconnect()
        {
            if (Chat == null)
                return;

            Chat.Leave(SteamChatLeaveReason.Left);
            Chat = null;
        }

        public override void Leave()
        {
            Disconnect();
            base.Leave();
        }

        public override void SendHistory(Connection connection)
        {
            base.SendHistory(connection);

            if (Chat == null)
                connection.SendSysMessage("Not connected to Steam.");
        }

        public override void Update()
        {
            if (!IsActive)
            {
                Chat?.Leave(SteamChatLeaveReason.Left);

                return;
            }

            if (Chat != null && _lastMessage.Elapsed >= TimeSpan.FromMinutes(30))
            {
                Program.Logger.Info("Rejoining " + RoomInfo.ShortName);
                _lastMessage.Restart();
                Chat.Leave(SteamChatLeaveReason.Disconnected);
                return;
            }

            if (Program.Steam.Status != Steam.ConnectionStatus.Connected || Chat != null)
                return;
            
            _hasConnected = false;
            Chat = Program.Steam.Bot.Join(SteamId);

            Chat.OnEnter += sender =>
            {
                _hasConnected = true;
                Program.Logger.Info("Entered " + RoomInfo.ShortName);
                SendPersistentSysMessage("Connected to Steam.");
            };

            Chat.OnLeave += (sender, reason) =>
            {
                if (_hasConnected)
                {
                    _hasConnected = false;
                    Program.Logger.Info("Left " + RoomInfo.ShortName + ": " + reason);
                    SendPersistentSysMessage("Lost connection to Steam.");
                }

                Chat = null;
            };

            Chat.OnMessage += HandleMessage;
            Chat.OnUserEnter += HandleEnter;
            Chat.OnUserLeave += HandleLeave;
        }

        private void SendPersistentSysMessage(string str)
        {
            var line = new ChatLine(Util.GetCurrentTimestamp(), RoomInfo.ShortName, "Steam", Program.Settings.PersonaName, "0", "", str, false);
            base.SendLine(line);
        }

        private void HandleMessage(SteamChat chat, SteamPersona user, string message)
        {
            _lastMessage.Restart();

            var senderName = user.DisplayName;
            var senderId = user.Id.ConvertToUInt64().ToString("D");
            var inGame = user.Playing != null && user.Playing.ToUInt64() != 0;

            var line = new ChatLine(Util.GetCurrentTimestamp(), RoomInfo.ShortName, "Steam", senderName, senderId, "", message, inGame);
            SendLine(line);

            Command.Handle(new CommandTarget(this, user), message, "~");
        }

        private void HandleEnter(SteamChat chat, SteamPersona user)
        {
            _lastMessage.Restart();

            var message = user.DisplayName + " entered chat.";

            var line = new StateLine(
                Util.GetCurrentTimestamp(),
                RoomInfo.ShortName,
                "Enter",
                user.DisplayName, user.Id.ConvertToUInt64().ToString("D"), "Steam", "",
                "", "0", "", "",
                message);

            SendLine(line);
        }

        private void HandleLeave(SteamChat chat, SteamPersona user, SteamChatLeaveReason reason, SteamPersona sourceUser)
        {
            _lastMessage.Restart();

            var message = user.DisplayName;
            switch (reason)
            {
                case SteamChatLeaveReason.Left:
                    message += " left chat.";
                    break;
                case SteamChatLeaveReason.Disconnected:
                    message += " disconnected.";
                    break;
                case SteamChatLeaveReason.Kicked:
                    message += $" was kicked by {sourceUser.DisplayName}.";
                    break;
                case SteamChatLeaveReason.Banned:
                    message += $" was banned by {sourceUser.DisplayName}.";
                    break;
            }

            var by = sourceUser != null ? sourceUser.DisplayName : "";
            var byId = sourceUser?.Id.ConvertToUInt64().ToString("D") ?? "0";
            var byType = sourceUser != null ? "Steam" : "";

            var line = new StateLine(
                Util.GetCurrentTimestamp(),
                RoomInfo.ShortName,
                reason.ToString(),
                user.DisplayName, user.Id.ConvertToUInt64().ToString("D"), "Steam", "",
                by, byId, byType, "",
                message);

            SendLine(line);
        }
    }
}
