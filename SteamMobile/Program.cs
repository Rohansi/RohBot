using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly ILog Logger = LogManager.GetLogger("Steam");
        private static readonly ILog ChatLogger = LogManager.GetLogger("Chat");

        private static WebSocketServer server;
        private static List<WebSocketSession> clients = new List<WebSocketSession>();
        private static SteamChat current = null;

        private static readonly SteamID MainChat = new SteamID(103582791430091926); // FPP 103582791430091926 // Test 103582791433607509
        private static readonly Dictionary<ulong, LinkedList<Tuple<string, string>>> ChatHistory = new Dictionary<ulong, LinkedList<Tuple<string, string>>>();

        private static void Main(string[] args)
        {
            try
            {
                dynamic settings = JsonConvert.DeserializeObject(File.ReadAllText("Account.json"));

                Steam.Login((string)settings.Username, (string)settings.Password);

                Steam.OnLoginSuccess = () =>
                {
                    Steam.Friends.SetPersonaName((string)settings.PersonaName);
                    Steam.Friends.SetPersonaState(EPersonaState.Online);
                };

                /*Steam.OnChatInvite = (chat, sender) =>
                {
                    var ch = Steam.Join(chat);
                    ch.OnMessage = HandleMessage;
                    ch.OnUserEnter = HandleEnter;
                    ch.OnUserLeave = HandleLeave;
                };*/

                Steam.OnPrivateEnter = chat =>
                {
                    chat.OnMessage = HandleMessage;
                    chat.OnUserEnter = HandleEnter;
                    chat.OnUserLeave = HandleLeave;
                };

                Steam.OnFriendRequest = user => Steam.Friends.AddFriend(user);

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
                    if (Steam.Status == Steam.ConnectionStatus.Connected && Steam.Chats.FirstOrDefault(c => c.RoomId == SteamUtil.ChatFromClan(MainChat)) == null)
                        JoinMainChat();

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                Logger.Fatal("Unhandled exception: " + e);
            }

            Steam.Abort();
        }

        private static void JoinMainChat()
        {
            var chat = Steam.Join(MainChat);
            chat.OnMessage = (source, sender, message) =>
            {
                ChatLogger.Info(JsonConvert.SerializeObject(new { Type = "Msg", Sender = sender.Render(), Name = Steam.Friends.GetFriendPersonaName(sender), Message = message }));
                Logger.InfoFormat("{0}: {1}", Steam.Friends.GetFriendPersonaName(sender), message);
                HandleMessage(source, sender, message);
            };
            chat.OnUserEnter = (source, user) =>
            {
                ChatLogger.Info(JsonConvert.SerializeObject(new { Type = "Join", Sender = user.Render() }));
                Logger.InfoFormat("{0} Joined", Steam.Friends.GetFriendPersonaName(user));
                HandleEnter(source, user);
            };
            chat.OnUserLeave = (source, user, reason) =>
            {
                ChatLogger.Info(JsonConvert.SerializeObject(new { Type = "Leave", Sender = user.Render(), Reason = reason.ToString() }));
                Logger.InfoFormat("{0} Left: {1}", Steam.Friends.GetFriendPersonaName(user), reason.ToString());
                HandleLeave(source, user, reason);
            };
        }

        private static void HandleMessage(SteamChat sender, SteamID messageSender, string message)
        {
            LogMessage(sender, Steam.Friends.GetFriendPersonaName(messageSender), message);

            if (sender != current)
                return;

            dynamic msg = new { Type = "message", Sender = WebUtility.HtmlEncode(Steam.Friends.GetFriendPersonaName(messageSender)), Message = WebUtility.HtmlEncode(message) };
            foreach (var c in clients)
                SendObject(c, msg);
        }

        private static void HandleLeave(SteamChat sender, SteamID user, UserLeaveReason reason)
        {
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
                    message += " was kicked.";
                    break;
                case UserLeaveReason.Banned:
                    message += " was banned.";
                    break;
            }

            LogMessage(sender, "*", message);

            if (sender != current)
                return;

            dynamic msg = new { Type = "message", Sender = "*", Message = WebUtility.HtmlEncode(message) };
            foreach (var c in clients)
                SendObject(c, msg);
        }

        private static void HandleEnter(SteamChat sender, SteamID user)
        {
            var message = Steam.Friends.GetFriendPersonaName(user) + " entered chat.";
            LogMessage(sender, "*", message);

            if (sender != current)
                return;

            dynamic msg = new { Type = "message", Sender = "*", Message = WebUtility.HtmlEncode(message) };
            foreach (var c in clients)
                SendObject(c, msg);
        }

        private static void OnConnected(WebSocketSession session)
        {
            clients.Add(session);
        }

        private static void OnDisconnect(WebSocketSession session, SuperSocket.SocketBase.CloseReason reason)
        {
            clients.Remove(session);
        }

        private static void OnReceive(WebSocketSession session, string message)
        {
            dynamic obj = JsonConvert.DeserializeObject(message);
            if (obj == null)
                return;
            switch ((string)obj.Type)
            {
                case "friendList":
                    {
                        current = null;
                        dynamic msg = new
                        {
                            Type = "friendList",
                            Friends = Steam.GetFriends()
                                           .Where(i => Steam.Friends.GetFriendPersonaState(i) != EPersonaState.Offline)
                                           .Select(i => new { Id = i.ConvertToUInt64().ToString(), Name = Steam.Friends.GetFriendPersonaName(i) })
                                           .Concat(Steam.GetClans()
                                                        .Select(c => new { Id = SteamUtil.ChatFromClan(c).ConvertToUInt64().ToString(), Name = Steam.GetClanName(c) }))
                        };
                        SendObject(session, msg);
                        break;
                    }

                case "conversationList":
                    {
                        current = null;
                        dynamic msg = new
                        {
                            Type = "conversationList",
                            Conversations = Steam.Chats.Select(c => new { Id = c.RoomId.ConvertToUInt64().ToString(), Name = c.Title })
                        };
                        SendObject(session, msg);
                        break;
                    }

                case "openChat":
                    {
                        var id = new SteamID((ulong)obj.Id);
                        var chat = Steam.Chats.FirstOrDefault(c => c.RoomId == id);
                        if (chat == null)
                        {
                            chat = Steam.Join(id);
                            chat.OnMessage = HandleMessage;
                            chat.OnUserEnter = HandleEnter;
                            chat.OnUserLeave = HandleLeave;
                        }
                        current = chat;
                        var historyId = current.RoomId.ConvertToUInt64();
                        var history = ChatHistory.ContainsKey(historyId) ? ChatHistory[historyId] : new LinkedList<Tuple<string, string>>();
                        dynamic msg = new { Type = "openChat", Title = chat.Title, History = history.Select(t => Tuple.Create(WebUtility.HtmlEncode(t.Item1), WebUtility.HtmlEncode(t.Item2))) };
                        SendObject(session, msg);
                        break;
                    }

                case "message":
                    {
                        if (current == null || ((string)obj.Message).Length == 0)
                            break;

                        current.Send((string)obj.Message);
                        LogMessage(current, Steam.Friends.GetPersonaName(), (string)obj.Message);
                        dynamic msg = new { Type = "message", Sender = WebUtility.HtmlEncode(Steam.Friends.GetPersonaName()), Message = WebUtility.HtmlEncode((string)obj.Message) };
                        SendObject(session, msg);
                        break;
                    }
            }
        }

        public static void SendObject(WebSocketSession context, dynamic obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            context.Send(str);
        }

        private static void LogMessage(SteamChat chat, string sender, string message)
        {
            var id = chat.RoomId.ConvertToUInt64();
            LinkedList<Tuple<string, string>> history;
            if (!ChatHistory.TryGetValue(id, out history))
                history = new LinkedList<Tuple<string, string>>();
            if (history.Count >= 150)
                history.RemoveFirst();
            history.AddLast(Tuple.Create(sender, message));
            ChatHistory[id] = history;
        }
    }
}
