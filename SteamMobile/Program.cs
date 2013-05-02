using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using EzSteam;
using Newtonsoft.Json;
using SteamKit2;
using Fleck;
using log4net;

namespace SteamMobile
{
    static class Program
    {
        public static readonly ILog Logger = LogManager.GetLogger("Steam");
        public static readonly ILog ChatLogger = LogManager.GetLogger("Chat");

        public static readonly DateTime StartTime = DateTime.Now;

        private static WebSocketServer server;
        public static Dictionary<Guid, Session> Sessions = new Dictionary<Guid, Session>();

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

            server = new WebSocketServer("ws://0.0.0.0:12000");
            server.Start(socket =>
            {
                socket.OnOpen = () => OnConnected(socket);
                socket.OnClose = () => OnDisconnect(socket);
                socket.OnMessage = message => OnReceive(socket, message);
            });

            Steam.Initialize(Settings.Username, Settings.Password);

            while (true)
            {
                if (Steam.Status == Steam.ConnectionStatus.Disconnected)
                    MainChat = null;

                if (Steam.Status == Steam.ConnectionStatus.Connected && MainChat == null)
                    JoinMainChat();

                Steam.Update();
                Ticker.Update();

                // HACK: fix weird fleck behavior
                lock (Sessions)
                    Sessions.RemoveAll(kvp => !kvp.Value.Socket.IsAvailable);

                // Warnings for connection status
                if (Steam.Status != Steam.ConnectionStatus.Connected || MainChat == null)
                {
                    var message = "";
                    if (MainChat == null)
                        message = "RohBot is not in its chat room.";
                    if (Steam.Status != Steam.ConnectionStatus.Connected)
                        message = "RohBot is not connected to Steam.";

                    foreach (var session in Sessions.Values.ToList())
                    {
                        SendSysMessage(session, message);
                    }
                }

                // Keep persona states up to date
                if (Steam.Status == Steam.ConnectionStatus.Connected)
                    Steam.Bot.SteamFriends.RequestFriendInfo(Accounts.GetIds());

                System.Threading.Thread.Sleep(2500);
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

            if (messageSender == Steam.Bot.PersonaId && message.StartsWith("["))
            {
                var nameEnd = message.IndexOf(']');
                senderName = message.Substring(1, nameEnd - 1);
                message = message.Substring(nameEnd + 2);
            }

            var line = new ChatLine(Util.GetCurrentUnixTimestamp(), "Steam", senderName, message);
            LogMessage(line);

            foreach (var sesion in Sessions.Values.ToList())
            {
                SendHistoryLine(sesion, line);
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

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), reason.ToString(), Steam.GetName(user), sourceUser != null ? Steam.GetName(sourceUser) : "", message);
            LogMessage(line);

            foreach (var s in Sessions.Values.ToList())
            {
                SendHistoryLine(s, line);
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

            var line = new StateLine(Util.GetCurrentUnixTimestamp(), "Enter", Steam.GetName(user), "", message);
            LogMessage(line);

            foreach (var s in Sessions.Values.ToList())
            {
                SendHistoryLine(s, line);
            }
        }

        private static void OnConnected(IWebSocketConnection socket)
        {
            lock (Sessions)
                Sessions.Add(socket.ConnectionInfo.Id, new Session(socket));
        }

        private static void OnDisconnect(IWebSocketConnection socket)
        {
            lock (Sessions)
                Sessions.Remove(socket.ConnectionInfo.Id);
        }

        private static void OnReceive(IWebSocketConnection socket, string message)
        {
            Session session;

            try
            {
                lock (Sessions)
                    session = Sessions[socket.ConnectionInfo.Id];
            }
            catch
            {
                socket.Close();
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
                Logger.ErrorFormat("Bad packet from {0}:{1}: '{2}' {3}", session.Name, session.RemoteAddress, message, e);
            }
        }

        public static bool Kick(string name, out string res)
        {
            var account = Accounts.Find(name);

            if (account == null)
            {
                res = "Account not found.";
                return false;
            }

            if (account.Permissions.HasFlag(Permissions.BanProof))
            {
                res = "Account can not be kicked.";
                return false;
            }

            var count = 0;
            var kickList = Sessions.Values.Where(s => s.Account == account).ToList();

            if (kickList.Count == 0)
            {
                res = "Nothing to kick.";
                return true;
            }

            foreach (var session in kickList)
            {
                session.Socket.Close();
                count++;
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

        public static void SendHistoryLine(Session session, HistoryLine line)
        {
            var msg = new Packets.Message { Line = line };
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
