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
        private static Dictionary<string, Session> sessions = new Dictionary<string, Session>();

        private static readonly SteamID MainChatId = SteamUtil.ChatFromClan(new SteamID(103582791430091926)); // FPP 103582791430091926 // Test 103582791433607509
        private static SteamChat MainChat;

        private static readonly Dictionary<ulong, LinkedList<Tuple<string, string>>> ChatHistory = new Dictionary<ulong, LinkedList<Tuple<string, string>>>();

        private static readonly dynamic Settings = JsonConvert.DeserializeObject(File.ReadAllText("Account.json"));

        private static void Main(string[] args)
        {
            try
            {
                Steam.Login((string)Settings.Username, (string)Settings.Password);

                Steam.OnLoginSuccess = () =>
                {
                    Steam.Friends.SetPersonaName((string)Settings.PersonaName);
                    Steam.Friends.SetPersonaState(EPersonaState.Online);
                };

                Steam.OnChatInvite = (chat, sender) =>
                {
                    var ch = Steam.Join(chat);
                    ch.OnMessage = HandleMessage;
                    ch.OnUserEnter = HandleEnter;
                    ch.OnUserLeave = HandleLeave;
                };

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
                    if (Steam.Status == Steam.ConnectionStatus.Connected && MainChat == null)
                        JoinMainChat();

                    System.Threading.Thread.Sleep(5000);
                }
            }
            catch (Exception e)
            {
                Logger.Fatal("Unhandled exception: " + e);
            }

            Logger.Info("Process exiting");
            Steam.Abort();
        }

        private static void JoinMainChat()
        {
            var chat = Steam.Join(MainChatId);
            chat.OnMessage = (source, sender, message) =>
            {
                var o = new
                {
                    Time = Util.GetCurrentUnixTimestamp(),
                    Type = "Msg",
                    Sender = sender.Render(),
                    Name = Steam.Friends.GetFriendPersonaName(sender),
                    Message = message
                };
                ChatLogger.Info(JsonConvert.SerializeObject(o));
                HandleMessage(source, sender, message);
            };
            chat.OnUserEnter = (source, user) =>
            {
                var o = new
                {
                    Time = Util.GetCurrentUnixTimestamp(),
                    Type = "Join",
                    Sender = user.Render()
                };
                ChatLogger.Info(JsonConvert.SerializeObject(o));
                HandleEnter(source, user);
            };
            chat.OnUserLeave = (source, user, reason) =>
            {
                var o = new
                {
                    Time = Util.GetCurrentUnixTimestamp(),
                    Type = "Leave",
                    Sender = user.Render(),
                    Reason = reason.ToString()
                };
                ChatLogger.Info(JsonConvert.SerializeObject(o));
                HandleLeave(source, user, reason);
            };
            chat.OnEnter = source =>
            {
                MainChat = chat;
            };
            chat.OnLeave = source =>
            {
                MainChat = null;
            };
        }

        private static void HandleMessage(SteamChat sender, SteamID messageSender, string message)
        {
            LogMessage(sender, Steam.Friends.GetFriendPersonaName(messageSender), message);

            var s = WebUtility.HtmlEncode(Steam.Friends.GetFriendPersonaName(messageSender));
            var m = WebUtility.HtmlEncode(message);

            foreach (var sesion in sessions)
            {
                if (sender == sesion.Value.CurrentChat)
                    SendChatMessage(sesion.Value.Socket, s, m);
            }
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

            dynamic msg = new { Type = "message", Sender = "*", Message = WebUtility.HtmlEncode(message) };
            foreach (var s in sessions)
            {
                if (sender == s.Value.CurrentChat)
                    SendObject(s.Value.Socket, msg);
            }
        }

        private static void HandleEnter(SteamChat sender, SteamID user)
        {
            var message = Steam.Friends.GetFriendPersonaName(user) + " entered chat.";
            LogMessage(sender, "*", message);

            dynamic msg = new { Type = "message", Sender = "*", Message = WebUtility.HtmlEncode(message) };
            foreach (var s in sessions)
            {
                if (sender == s.Value.CurrentChat)
                    SendObject(s.Value.Socket, msg);
            }
        }

        private static void OnConnected(WebSocketSession session)
        {
            sessions.Add(session.SessionID, new Session(session));
        }

        private static void OnDisconnect(WebSocketSession session, SuperSocket.SocketBase.CloseReason reason)
        {
            sessions.Remove(session.SessionID);
        }

        private static void OnReceive(WebSocketSession session, string message)
        {
            try
            {
                dynamic obj = JsonConvert.DeserializeObject(message);

                if (obj == null)
                    return;

                var s = sessions[session.SessionID];

                // can only login if we haven't already
                if (!s.Authenticated && (string)obj.Type != "login")
                    return;

                switch ((string)obj.Type)
                {
                    case "login":
                        {
                            if (Steam.Status != Steam.ConnectionStatus.Connected)
                            {
                                SendChatMessage(session, "*", "RohPod is not connected to Steam.");
                                session.CloseWithHandshake("");
                                return;
                            }

                            var user = (string)obj.Username;
                            var pass = (string)obj.Password;

                            try
                            {
                                if (s.Load(user, pass))
                                {
                                    Logger.InfoFormat("Login success from {0} for '{1}' using password '{2}'", session.RemoteEndPoint, user, pass);
                                    SendChatMessage(session, "*", string.Format("Logged in as {0}.", s.Name));
                                }
                                else
                                {
                                    Logger.InfoFormat("Login failed from {0} for '{1}' using password '{2}'", session.RemoteEndPoint, user, pass);
                                    SendChatMessage(session, "*", "Login failed.");
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.WarnFormat("Login error from {0} for '{1}' using password '{2}'\n{3}", session.RemoteEndPoint, user, pass, e);
                                SendChatMessage(session, "*", "Login failed.");
                            }

                            if (MainChat == null)
                            {
                                SendChatMessage(session, "*", "RohPod is not in its default chatroom.");
                                session.CloseWithHandshake(""); // TODO: temporary?
                                return;
                            }

                            s.CurrentChat = MainChat;

                            if (!s.HasBacklog)
                            {
                                SendChatBacklog(session, s.CurrentChat);
                                s.HasBacklog = true;
                            }

                            dynamic o = new { Type = "chatLock", CanChat = s.Permissions.HasFlag(Permissions.Chat) };
                            SendObject(session, o);

                            break;
                        }

                    case "message":
                        {
                            if (!s.Permissions.HasFlag(Permissions.Chat))
                                return;

                            if (s.CurrentChat == null || ((string)obj.Message).Length == 0)
                                return;

                            if ((string)obj.Message == "/chats")
                            {
                                obj.Message = string.Join(", ", Steam.Chats.Select(c => string.Format("{0} ({1})", c.Title, c.RoomId)));
                            }

                            if ((string)obj.Message == "/status")
                            {
                                obj.Message = Steam.Status.ToString();
                            }

                            var msg = string.Format("[{0}] {1}", s.Name, (string)obj.Message);
                            s.CurrentChat.Send(msg);
                            HandleMessage(s.CurrentChat, Steam.Client.SteamID, msg);
                            break;
                        }

                    case "ban":
                        {
                            if (!s.Permissions.HasFlag(Permissions.Ban))
                                return;

                            try
                            {
                                var target = (string)obj.Target;
                                Logger.InfoFormat("User '{0}' banning '{1}'", s.Name, target);

                                var res = Ban(target);
                                SendChatMessage(session, "*", res);
                            }
                            catch (Exception)
                            {
                                SendChatMessage(session, "*", "Failed to ban. Check logs.");
                                throw;
                            }

                            break;
                        }

                    /*case "friendList":
                        {
                            if (!s.Permissions.HasFlag(Permissions.FriendList))
                                break;

                            s.CurrentChat = null;
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
                            if (!s.Permissions.HasFlag(Permissions.ConversationList))
                                break;

                            s.CurrentChat = null;
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
                            if (!s.Permissions.HasFlag(Permissions.OpenChat))
                                break;

                            var id = new SteamID((ulong)obj.Id);
                            var chat = Steam.Chats.FirstOrDefault(c => c.RoomId == id);

                            if (chat == null)
                            {
                                chat = id == MainChatId ? MainChat : Steam.Join(id);

                                if (chat == null) // TODO: need to notify about this
                                    break;

                                chat.OnMessage = HandleMessage;
                                chat.OnUserEnter = HandleEnter;
                                chat.OnUserLeave = HandleLeave;
                            }

                            s.CurrentChat = chat;
                            SendChatBacklog(session, s.CurrentChat);
                            break;
                        }*/
                }
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Bad packet from {0}: '{1}' {2}", session.RemoteEndPoint, message, e);
            }
        }

        private static string Ban(string name)
        {
            name = name.ToLower();

            if (name == "rohan" || name == "guest")
                return "Cannot ban Rohan or Guest.";

            var res = Session.Ban(name);
            Kick(name);
            return res;
        }

        private static void Kick(string name)
        {
            name = name.ToLower();

            if (name == "rohan" || name == "guest")
                return;

            foreach (var session in sessions.Values)
            {
                if (session.Name.ToLower() == name)
                {
                    session.Socket.CloseWithHandshake("");
                }
            }
        }

        private static void SendChatBacklog(WebSocketSession session, SteamChat chat)
        {
            var historyId = chat.RoomId.ConvertToUInt64();
            var history = ChatHistory.ContainsKey(historyId) ? ChatHistory[historyId] : new LinkedList<Tuple<string, string>>();
            dynamic msg = new { Type = "openChat", Title = chat.Title, History = history.Select(t => Tuple.Create(WebUtility.HtmlEncode(t.Item1), WebUtility.HtmlEncode(t.Item2))) };
            SendObject(session, msg);
        }

        private static void SendChatMessage(WebSocketSession session, string sender, string message)
        {
            dynamic msg = new { Type = "message", Sender = sender, Message = message };
            SendObject(session, msg);
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
