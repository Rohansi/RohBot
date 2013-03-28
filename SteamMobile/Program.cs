using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using SteamKit2;
using SuperWebSocket;
using log4net;

namespace SteamMobile
{
    static class Program
    {
        public static readonly ILog Logger = LogManager.GetLogger("Steam");
        public static readonly ILog ChatLogger = LogManager.GetLogger("Chat");

        private static WebSocketServer server;
        public static Dictionary<string, Session> Sessions = new Dictionary<string, Session>();

        public static SteamChat MainChat;

        private static readonly LinkedList<HistoryLine> ChatHistory = new LinkedList<HistoryLine>();

        private static void Main()
        {
            try
            {
                Steam.Login(Settings.Username, Settings.Password);

                Steam.OnLoginSuccess = () =>
                {
                    Steam.Friends.SetPersonaName(Settings.PersonaName);
                    Steam.Friends.SetPersonaState(EPersonaState.Online);
                };

                Steam.OnDisconnected = () => MainChat = null;

                server = new WebSocketServer();
                if (!server.Setup(12000))
                {
                    Logger.Fatal("WebSocket Setup Failed");
                    return;
                }

                server.NewSessionConnected += OnConnected;
                server.SessionClosed += OnDisconnect;
                server.NewMessageReceived += OnReceive;
                server.Start();

                while (true)
                {
                    if (Steam.Status != Steam.ConnectionStatus.Connected)
                        MainChat = null;

                    if (Steam.Status == Steam.ConnectionStatus.Connected && MainChat == null)
                        JoinMainChat();

                    if (Steam.Frozen)
                        throw new Exception("Steam thread froze");

                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                Logger.Fatal("Unhandled exception: " + e);
            }

            Logger.Info("Process exiting");
            server.Stop();
            Steam.Abort();

            Environment.Exit(0);
        }

        private static void JoinMainChat()
        {
            var chat = Steam.Join(Settings.ChatId);
            chat.OnMessage = HandleMessage;
            chat.OnUserEnter = HandleEnter;
            chat.OnUserLeave = HandleLeave;
            chat.OnEnter = source => MainChat = chat;
            chat.OnLeave = source => MainChat = null;
        }

        public static void HandleMessage(SteamChat sender, SteamID messageSender, string message)
        {
            var o = new
            {
                Time = Util.GetCurrentUnixTimestamp(),
                Type = "Msg",
                Sender = messageSender.Render(),
                Name = SteamName.Get(messageSender),
                Message = message
            };
            ChatLogger.Info(JsonConvert.SerializeObject(o));

            var senderName = SteamName.Get(messageSender);
            LogMessage(new ChatLine(Util.GetCurrentUnixTimestamp(), senderName, message));

            foreach (var sesion in Sessions)
            {
                SendMessage(sesion.Value, senderName, message);
            }

            Command.Handle(CommandTarget.FromSteam(sender), message, "~");
        }

        private static void HandleLeave(SteamChat sender, SteamID user, UserLeaveReason reason, SteamID sourceUser)
        {
            var o = new
            {
                Time = Util.GetCurrentUnixTimestamp(),
                Type = "Leave",
                Sender = user.Render(),
                Reason = reason.ToString()
            };
            ChatLogger.Info(JsonConvert.SerializeObject(o));

            var message = SteamName.Get(user);
            switch (reason)
            {
                case UserLeaveReason.Left:
                    message += " left chat.";
                    break;
                case UserLeaveReason.Disconnected:
                    message += " disconnected.";
                    break;
                case UserLeaveReason.Kicked:
                    message += string.Format(" was kicked by {0}.", SteamName.Get(sourceUser));
                    break;
                case UserLeaveReason.Banned:
                    message += string.Format(" was banned by {0}.", SteamName.Get(sourceUser));
                    break;
            }

            LogMessage(new StatusLine(Util.GetCurrentUnixTimestamp(), message));

            foreach (var s in Sessions.Values)
            {
                SendStateChange(s, message);
            }
        }

        private static void HandleEnter(SteamChat sender, SteamID user)
        {
            var o = new
            {
                Time = Util.GetCurrentUnixTimestamp(),
                Type = "Join",
                Sender = user.Render()
            };
            ChatLogger.Info(JsonConvert.SerializeObject(o));

            var message = SteamName.Get(user) + " entered chat.";
            LogMessage(new StatusLine(Util.GetCurrentUnixTimestamp(), message));

            foreach (var s in Sessions.Values)
            {
                SendStateChange(s, message);
            }
        }

        private static void OnConnected(WebSocketSession session)
        {
            Sessions.Add(session.SessionID, new Session(session));
        }

        private static void OnDisconnect(WebSocketSession session, SuperSocket.SocketBase.CloseReason reason)
        {
            Sessions.Remove(session.SessionID);
        }

        private static void OnReceive(WebSocketSession conn, string message)
        {
            var session = Sessions[conn.SessionID];

            try
            {
                var packet = Packet.ReadFromMessage(message);

                if (packet == null)
                    return;

                // can only login if we haven't already
                if (!session.Authenticated && packet.Type != "login")
                    return;

                switch (packet.Type)
                {
                    case "login":
                        Packets.Login.Handle(session, packet);
                        break;

                    case "sendMessage":
                        Packets.SendMessage.Handle(session, packet);
                        break;

                    case "ban":
                        Packets.Ban.Handle(session, packet);
                        break;

                    case "userData":
                        Packets.UserData.Handle(session, packet);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Bad packet from {0}:{1}: '{2}' {3}", session.Name, conn.RemoteEndPoint.Address, message, e);
            }
        }

        public static string Ban(string name)
        {
            name = name.ToLower();

            string res;
            if (Session.Ban(name, out res))
                Kick(name);

            return res;
        }

        public static void Kick(string name)
        {
            name = name.ToLower();

            foreach (var session in Sessions.Values)
            {
                if (!session.Permissions.HasFlag(Permissions.BanProof) && session.Name.ToLower() == name)
                {
                    session.Socket.CloseWithHandshake("");
                }
            }
        }

        public static void SendHistory(Session session)
        {
            var lines = ChatHistory.Where(l =>
            {
                var w = l as WhisperLine;
                if (w != null)
                    return (w.Sender == session.Name || w.Receiver == session.Name);
                return true;
            });

            var msg = new Packets.ChatHistory { Lines = lines };
            Send(session, msg);
        }

        public static void SendMessage(Session session, string sender, string message)
        {
            var msg = new Packets.Message
            {
                Line = new ChatLine(Util.GetCurrentUnixTimestamp(), sender, message)
            };
            Send(session, msg);
        }

        public static void SendStateChange(Session session, string message)
        {
            var msg = new Packets.Message
            {
                Line = new StatusLine(Util.GetCurrentUnixTimestamp(), message)
            };
            Send(session, msg);
        }

        public static void SendWhisper(Session session, string sender, string receiver, string message)
        {
            var msg = new Packets.Message
            {
                Line = new WhisperLine(Util.GetCurrentUnixTimestamp(), sender, receiver, message)
            };
            Send(session, msg);
        }

        public static void SendSysMessage(Session session, string message)
        {
            var msg = new Packets.SysMessage
            {
                Date = Util.GetCurrentUnixTimestamp(),
                Content = WebUtility.HtmlEncode(message)
            };
            Send(session, msg);
        }

        public static void Send(Session session, Packet packet)
        {
            session.Socket.Send(Packet.WriteToMessage(packet));
        }

        public static void LogMessage(HistoryLine line)
        {
            if (ChatHistory.Count >= 150)
                ChatHistory.RemoveFirst();
            ChatHistory.AddLast(line);
        }
    }
}
