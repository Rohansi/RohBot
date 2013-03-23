using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

        //private static readonly SteamID MainChatId = SteamUtil.ChatFromClan(new SteamID(103582791433607509)); // Testin Stuff
        //private static readonly SteamID MainChatId = SteamUtil.ChatFromClan(new SteamID(103582791430091926)); // FP Programmers

        public static SteamChat MainChat;

        private static readonly LinkedList<ChatLine> ChatHistory = new LinkedList<ChatLine>();

        private static void Main(string[] args)
        {
            try
            {
                Steam.Login(Settings.Username, Settings.Password);

                Steam.OnLoginSuccess = () =>
                {
                    Steam.Friends.SetPersonaName(Settings.PersonaName);
                    Steam.Friends.SetPersonaState(EPersonaState.Online);
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
            chat.OnEnter = source =>
            {
                MainChat = chat;
            };
            chat.OnLeave = source =>
            {
                MainChat = null;
            };
        }

        public static void HandleMessage(SteamChat sender, SteamID messageSender, string message)
        {
            var o = new
            {
                Time = Util.GetCurrentUnixTimestamp(),
                Type = "Msg",
                Sender = messageSender.Render(),
                Name = Steam.Friends.GetFriendPersonaName(messageSender),
                Message = message
            };
            ChatLogger.Info(JsonConvert.SerializeObject(o));

            LogMessage(Steam.Friends.GetFriendPersonaName(messageSender), message);

            var senderName = WebUtility.HtmlEncode(Steam.Friends.GetFriendPersonaName(messageSender));
            message = WebUtility.HtmlEncode(message);

            foreach (var sesion in Sessions)
            {
                SendMessage(sesion.Value.Socket, senderName, message);
            }
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

            var message = Steam.Friends.GetFriendPersonaName(user);
            switch (reason)
            {
                case UserLeaveReason.Left:
                    message += " left chat.";
                    break;
                case UserLeaveReason.Disconnected:
                    message += " disconnected.";
                    break;
                case UserLeaveReason.Kicked:
                    message += string.Format(" was kicked by {0}.", Steam.Friends.GetFriendPersonaName(sourceUser));
                    break;
                case UserLeaveReason.Banned:
                    message += string.Format(" was banned by {0}.", Steam.Friends.GetFriendPersonaName(sourceUser));
                    break;
            }

            LogMessage("*", message);

            foreach (var s in Sessions.Values)
            {
                SendMessage(s.Socket, "*", message);
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

            var message = Steam.Friends.GetFriendPersonaName(user) + " entered chat.";
            LogMessage("*", message);

            foreach (var s in Sessions.Values)
            {
                SendMessage(s.Socket, "*", message);
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
            try
            {
                var session = Sessions[conn.SessionID];
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
                }
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Bad packet from {0}: '{1}' {2}", conn.RemoteEndPoint, message, e);
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

        public static void SendHistory(WebSocketSession session)
        {
            var msg = new Packets.ChatHistory
            {
                Lines = ChatHistory.Select(t => new ChatLine(t.Date, WebUtility.HtmlEncode(t.Sender), WebUtility.HtmlEncode(t.Content)))
            };
            Send(session, msg);
        }

        public static void SendMessage(WebSocketSession session, string sender, string message)
        {
            var msg = new Packets.Message
            {
                Date = Util.GetCurrentUnixTimestamp(),
                Sender = WebUtility.HtmlEncode(sender),
                Content = WebUtility.HtmlEncode(message)
            };
            Send(session, msg);
        }

        public static void Send(WebSocketSession context, Packet obj)
        {
            context.Send(Packet.WriteToMessage(obj));
        }

        public static void LogMessage(string sender, string message)
        {
            if (ChatHistory.Count >= 150)
                ChatHistory.RemoveFirst();
            ChatHistory.AddLast(new ChatLine(Util.GetCurrentUnixTimestamp(), sender, message));
        }
    }
}
