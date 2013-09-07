using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Fleck;
using log4net;

namespace SteamMobile
{
    static class Program
    {
        public static readonly ILog Logger = LogManager.GetLogger("Steam");

        public static readonly DateTime StartTime = DateTime.Now;

        private static WebSocketServer server;
        public static Dictionary<Guid, Session> Sessions = new Dictionary<Guid, Session>();
        public static Dictionary<string, GroupChat> Chats = new Dictionary<string, GroupChat>();
        public static LinkedList<HistoryLine> WhisperHistory = new LinkedList<HistoryLine>();

        private static void Main()
        {
            Logger.Info("Process starting");

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Logger.Fatal("Unhandled exception: " + args.ExceptionObject);
                Logger.Info("Process exiting");
            };

            ThreadPool.SetMaxThreads(5, 1);

            if (string.IsNullOrWhiteSpace(Settings.Cert))
            {
                server = new WebSocketServer("ws://0.0.0.0:12000/");
            }
            else
            {
                server = new WebSocketServer("wss://0.0.0.0:12000/");
                server.Certificate = new X509Certificate2(Settings.Cert, Settings.CertPass);
            }
            
            server.Start(socket =>
            {
                socket.OnOpen = () => OnConnected(socket);
                socket.OnClose = () => OnDisconnect(socket);
                socket.OnMessage = message => OnReceive(socket, message);
            });

            Steam.Initialize(Settings.Username, Settings.Password);

            while (true)
            {
                Steam.Update();
                Ticker.Update();

                // HACK: fix weird fleck behavior
                lock (Sessions)
                    Sessions.RemoveAll(kvp => !kvp.Value.Socket.IsAvailable);

                // Warnings for connection status
                if (Steam.Status != Steam.ConnectionStatus.Connected)
                {
                    Chats.Clear();

                    foreach (var session in Sessions.Values.ToList())
                    {
                        SendSysMessage(session, "RohBot is not connected to Steam.");
                    }
                }

                if (Steam.Status == Steam.ConnectionStatus.Connected)
                {
                    // Make sure group chats are current
                    foreach (var group in Chats.Where(group => !Settings.Chats.Keys.Contains(group.Key)).ToList())
                    {
                        group.Value.Leave();
                    }

                    foreach (var group in Settings.Chats.Where(group => !Chats.Keys.Contains(group.Key)).ToList())
                    {
                        Chats.Add(group.Key, new GroupChat(group.Key, group.Value));
                    }

                    Chats.RemoveAll(c => !c.Value.Active);

                    foreach (var group in Chats.Values.ToList())
                    {
                        group.Update();
                    }
                }

                Thread.Sleep(2500);
            }
        }

        public static void Exit(string reason)
        {
            Logger.FatalFormat("Exiting: {0}", reason);
            Process.GetCurrentProcess().Kill();
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

                    case "chatHistoryRequest":
                        Packets.ChatHistoryRequest.Handle(session, packet);
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
            GroupChat chat;

            if (!Chats.TryGetValue(session.Chat, out chat))
            {
                SendSysMessage(session, "RohBot is not in the current chat.");
                return;
            }

            var lines = chat.History.Concat(WhisperHistory).Where(l =>
            {
                var w = l as WhisperLine;
                if (w == null)
                    return true;
                return w.Sender == session.Name || w.Receiver == session.Name;
            }).OrderByDescending(l => l.Date).Take(100).Reverse();

            var msg = new Packets.ChatHistory { Requested = false, Chat = chat.Name, Lines = lines.ToList() };
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

        public static void AddWhisper(WhisperLine line)
        {
            if (WhisperHistory.Count > 500)
                WhisperHistory.RemoveFirst();
            WhisperHistory.AddLast(line);
        }
    }
}
