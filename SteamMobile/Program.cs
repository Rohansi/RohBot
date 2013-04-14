using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using EzSteam;
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

        public static Chat MainChat { get; private set; }

        private static readonly LinkedList<HistoryLine> ChatHistory = new LinkedList<HistoryLine>();

        private static void Main()
        {
            Logger.Info("Process starting");

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Logger.Fatal("Unhandled exception: " + args.ExceptionObject);
                Logger.Info("Process exiting");
            };

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

            Steam.Initialize(Settings.Username, Settings.Password);

            while (true)
            {
                if (Steam.Status == Steam.ConnectionStatus.Disconnected)
                    MainChat = null;

                if (Steam.Status == Steam.ConnectionStatus.Connected && MainChat == null)
                    JoinMainChat();

                Steam.Update();
                Ticker.Update();

                System.Threading.Thread.Sleep(5000);
            }
        }

        private static void JoinMainChat()
        {
            MainChat = Steam.Bot.Join(Settings.ChatId);
            MainChat.EchoSelf = true;
            MainChat.OnMessage += HandleMessage;
            MainChat.OnUserEnter += HandleEnter;
            MainChat.OnUserLeave += HandleLeave;
            MainChat.OnLeave += (source, reason) => MainChat = null;
        }

        public static void Exit(string reason)
        {
            Logger.FatalFormat("Exiting: {0}", reason);
            Process.GetCurrentProcess().Kill();
        }

        private static readonly List<SteamID> Ignored = new List<SteamID>()
        {
            new SteamID(76561198060006931), // SweetiBot
            new SteamID(76561198060797164)  // ScootaBorg
        };

        private static void HandleMessage(Chat sender, SteamID messageSender, string message)
        {
            var o = new
            {
                Time = Util.GetCurrentUnixTimestamp(),
                Type = "Msg",
                Sender = messageSender.Render(),
                Name = Steam.GetName(messageSender),
                Message = message
            };
            ChatLogger.Info(JsonConvert.SerializeObject(o));

            var senderName = Steam.GetName(messageSender);
            LogMessage(new ChatLine(Util.GetCurrentUnixTimestamp(), senderName, message));

            foreach (var sesion in Sessions.Values.ToList())
            {
                SendMessage(sesion, senderName, message);
            }

            if (Ignored.Contains(messageSender) || messageSender == Steam.Bot.PersonaId)
                return;

            Command.Handle(CommandTarget.FromSteam(sender, messageSender), message, "~");
        }

        private static void HandleLeave(Chat sender, SteamID user, Chat.LeaveReason reason, SteamID sourceUser)
        {
            var o = new
            {
                Time = Util.GetCurrentUnixTimestamp(),
                Type = "Leave",
                Sender = user.Render(),
                Reason = reason.ToString()
            };
            ChatLogger.Info(JsonConvert.SerializeObject(o));

            var message = Steam.GetName(user);
            switch (reason)
            {
                case Chat.LeaveReason.Left:
                    message += " left chat.";
                    break;
                case Chat.LeaveReason.Disconnected:
                    message += " disconnected.";
                    break;
                case Chat.LeaveReason.Kicked:
                    message += string.Format(" was kicked by {0}.", Steam.GetName(sourceUser));
                    break;
                case Chat.LeaveReason.Banned:
                    message += string.Format(" was banned by {0}.", Steam.GetName(sourceUser));
                    break;
            }

            LogMessage(new StatusLine(Util.GetCurrentUnixTimestamp(), message));

            foreach (var s in Sessions.Values.ToList())
            {
                SendStateChange(s, message);
            }
        }

        private static void HandleEnter(Chat sender, SteamID user)
        {
            var o = new
            {
                Time = Util.GetCurrentUnixTimestamp(),
                Type = "Join",
                Sender = user.Render()
            };
            ChatLogger.Info(JsonConvert.SerializeObject(o));

            var message = Steam.GetName(user) + " entered chat.";
            LogMessage(new StatusLine(Util.GetCurrentUnixTimestamp(), message));

            foreach (var s in Sessions.Values.ToList())
            {
                SendStateChange(s, message);
            }
        }

        private static void OnConnected(WebSocketSession session)
        {
            lock (Sessions)
                Sessions.Add(session.SessionID, new Session(session));
        }

        private static void OnDisconnect(WebSocketSession session, SuperSocket.SocketBase.CloseReason reason)
        {
            lock (Sessions)
                Sessions.Remove(session.SessionID);
        }

        private static void OnReceive(WebSocketSession conn, string message)
        {
            Session session;

            try
            {
                lock (Sessions)
                    session = Sessions[conn.SessionID]; 
            }
            catch
            {
                conn.CloseWithHandshake("");
                return;
            }

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

        public static bool Kick(string name, out string res)
        {
            name = name.ToLower();

            var count = 0;
            var kickList = Sessions.Values.Where(s => s.Name.ToLower() == name).ToList();

            if (kickList.Count == 0)
            {
                res = "Nothing to kick.";
                return true;
            }

            foreach (var session in kickList)
            {
                if (session.Permissions.HasFlag(Permissions.BanProof))
                {
                    res = "Account can not be kicked.";
                    return false;
                }

                if (session.Name.ToLower() == name)
                {
                    session.Socket.CloseWithHandshake("");
                    count++;
                }
            }

            res = string.Format("Kicked {0} session(s).", count);
            return true;
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
