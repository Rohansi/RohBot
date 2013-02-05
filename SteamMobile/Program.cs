using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using SteamKit2;
using Alchemy;
using Alchemy.Classes;

namespace SteamMobile
{
    static class Program
    {
        private static WebSocketServer server;
        private static List<UserContext> clients = new List<UserContext>();
        private static SteamChat current = null;

        private static void Main(string[] args)
        {
            dynamic settings = JsonConvert.DeserializeObject(File.ReadAllText("Account.json"));

            Steam.Login((string)settings.Username, (string)settings.Password);

            Steam.OnLoginSuccess = () =>
            {
                Steam.Friends.SetPersonaName((string)settings.PersonaName);
                Steam.Friends.SetPersonaState(EPersonaState.Online);

                /*var chat = Steam.Join(new SteamID(103582791433607509)); // test group 103582791433607509
                chat.OnMessage = HandleMessage;
                chat.OnUserEnter = HandleEnter;
                chat.OnUserLeave = HandleLeave;*/
            };

            Steam.OnChatInvite = (chat, sender) =>
            {
                var ch = Steam.Join(chat);
                ch.OnMessage = HandleMessage;
                ch.OnUserEnter = HandleEnter;
                ch.OnUserLeave = HandleLeave;
            };

            Steam.OnPrivateEnter = steamChat =>
            {
                steamChat.OnMessage = HandleMessage;
                steamChat.OnUserEnter = HandleEnter;
                steamChat.OnUserLeave = HandleLeave;
            };

            Steam.OnFriendRequest = user => Steam.Friends.AddFriend(user);

            server = new WebSocketServer(1200)
            {
                OnConnected = OnConnected,
                OnDisconnect = OnDisconnect,
                OnReceive = OnReceive
            };
            server.Start();

            while (true)
            {
                System.Threading.Thread.Sleep(1);
            }
        }

        private static void HandleMessage(SteamChat source, SteamID messageSender, string message)
        {
            if (source != current)
                return;
            dynamic msg = new { Type = "message", Sender = WebUtility.HtmlEncode(Steam.Friends.GetFriendPersonaName(messageSender)), Message = WebUtility.HtmlEncode(message) };
            foreach (var c in clients)
                SendObject(c, msg);
        }

        private static void HandleLeave(SteamChat source, SteamID user, UserLeaveReason reason)
        {
            var name = WebUtility.HtmlEncode(Steam.Friends.GetFriendPersonaName(user));
            var message = "";

            switch (reason)
            {
                case UserLeaveReason.Left:
                    message = "left chat.";
                    break;
                case UserLeaveReason.Disconnected:
                    message = "disconnected.";
                    break;
                case UserLeaveReason.Kicked:
                    message = "was kicked.";
                    break;
                case UserLeaveReason.Banned:
                    message = "was banned.";
                    break;
            }

            message = name + " " + message;
            source.AddHistory("*", message);

            if (source != current)
                return;

            dynamic msg = new { Type = "message", Sender = "*", Message = message };
            foreach (var c in clients)
                SendObject(c, msg);
        }

        private static void HandleEnter(SteamChat source, SteamID user)
        {
            var name = WebUtility.HtmlEncode(Steam.Friends.GetFriendPersonaName(user));
            var message = name + " entered chat.";
            source.AddHistory("*", message);

            if (source != current)
                return;

            dynamic msg = new { Type = "message", Sender = "*", Message = message };
            foreach (var c in clients)
                SendObject(c, msg);
        }

        private static void OnConnected(UserContext context)
        {
            Console.WriteLine("Connected");
            clients.Add(context);
        }

        private static void OnDisconnect(UserContext context)
        {
            Console.WriteLine("Disconnected");
            clients.Remove(context);
        }

        private static void OnReceive(UserContext context)
        {
            var str = context.DataFrame.ToString();
            dynamic obj = JsonConvert.DeserializeObject(str);
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
                                                    .Select(c => new { Id = c.ConvertToUInt64().ToString(), Name = Steam.GetClanName(c) }))
                    };
                    SendObject(context, msg);
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
                    SendObject(context, msg);
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
                    dynamic msg = new { Type = "openChat", Title = chat.Title, History = chat.History.Select(t => Tuple.Create(WebUtility.HtmlEncode(t.Item1), WebUtility.HtmlEncode(t.Item2))) };
                    SendObject(context, msg);
                    break;
                }

                case "message":
                {
                    if (current == null || ((string)obj.Message).Length == 0)
                        break;
                    current.Send((string)obj.Message);
                    dynamic msg = new { Type = "message", Sender = WebUtility.HtmlEncode(Steam.Friends.GetPersonaName()), Message = WebUtility.HtmlEncode((string)obj.Message) };
                    SendObject(context, msg);
                    break;
                }
            }
        }

        public static void SendObject(UserContext context, dynamic obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            context.Send(str);
        }
    }
}
