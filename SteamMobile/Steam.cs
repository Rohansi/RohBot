using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using SteamKit2;
using SteamKit2.Internal;
using log4net;

namespace SteamMobile
{
    static class Steam
    {
        public enum ConnectionStatus
        {
            Connected,
            Connecting,
            Disconnected,
            ConnectFailed,
            LoginFailed
        }

        public delegate void LoginSuccess();
        public delegate void PrivateEnterEvent(SteamChat chat);
        public delegate void ChatInviteEvent(SteamID chat, SteamID invitedBy);
        public delegate void FriendRequestedEvent(SteamID user);

        public static SteamClient Client;
        public static SteamUser User;
        public static SteamFriends Friends;

        public static ConnectionStatus Status { get; private set; }

        public static LoginSuccess OnLoginSuccess = null;
        public static PrivateEnterEvent OnPrivateEnter = null;
        public static ChatInviteEvent OnChatInvite = null;
        public static FriendRequestedEvent OnFriendRequest = null;

        public static ReadOnlyCollection<SteamChat> Chats
        {
            get { return new ReadOnlyCollection<SteamChat>(chats); }
        }

        private static readonly ILog Logger = LogManager.GetLogger("Steam");

        private static string username;
        private static string password;

        private static List<SteamChat> chats;

        private static readonly Thread UpdateThread;
        private static readonly Dictionary<SteamID, string> ClanNames = new Dictionary<SteamID, string>(); 

        static Steam()
        {
            Reset();

            UpdateThread = new Thread(Run);
            UpdateThread.Start();
        }

        public static void Abort()
        {
            UpdateThread.Abort();
        }

        public static void Reset()
        {
            if (Client != null)
                Client.Disconnect();

            Client = new SteamClient();
            User = Client.GetHandler<SteamUser>();
            Friends = Client.GetHandler<SteamFriends>();
            Client.AddHandler(new SteamHandlers());

            Status = ConnectionStatus.Disconnected;
            chats = new List<SteamChat>();
        }

        public static void Login(string user, string pass)
        {
            username = user;
            password = pass;

            Client.Connect(Dns.GetHostAddresses("cm0.steampowered.com").FirstOrDefault());
            Status = ConnectionStatus.Connecting;
        }

        private static void Run()
        {
            while (true)
            {
                if (Status == ConnectionStatus.LoginFailed)
                    Thread.Sleep(10000);

                if (Status != ConnectionStatus.Connected && Status != ConnectionStatus.Connecting)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        try
                        {
                            Logger.Info("Connecting...");
                            Reset();
                            Login(username, password);
                        }
                        catch (Exception e)
                        {
                            Logger.Info("Login failed", e);
                            throw new Exception("Closing");
                        }
                    }

                    continue;
                }

                Thread.Sleep(1);
                var msg = Client.WaitForCallback(true);

                msg.Handle<SteamClient.DisconnectedCallback>(callback =>
                {
                    Logger.Info("Disconnected");
                    Status = ConnectionStatus.Disconnected;
                });

                msg.Handle<SteamClient.ConnectedCallback>(callback =>
                {
                    if (callback.Result != EResult.OK)
                    {
                        Status = ConnectionStatus.ConnectFailed;
                        return;
                    }

                    Logger.Info("Logging in...");
                    User.LogOn(new SteamUser.LogOnDetails
                    {
                        Username = username,
                        Password = password
                    });
                });

                msg.Handle<SteamUser.LoggedOnCallback>(callback =>
                {
                    if (callback.Result != EResult.OK)
                    {
                        Status = ConnectionStatus.LoginFailed;
                        Logger.Fatal(string.Format("Login failed: {0}", callback.Result));
                    }
                    else
                    {
                        Logger.Info("Logged in");

                        if (OnLoginSuccess != null)
                            OnLoginSuccess();
                    }
                });

                msg.Handle<SteamUser.LoginKeyCallback>(callback =>
                {
                    Status = ConnectionStatus.Connected;
                });

                msg.Handle<SteamUser.LoggedOffCallback>(callback =>
                {
                    Status = ConnectionStatus.Disconnected;
                });

                // someone messaged us, make sure we have a SteamChat instance setup
                msg.Handle<SteamFriends.FriendMsgCallback>(callback =>
                {
                    if (callback.EntryType != EChatEntryType.ChatMsg) return;

                    if (chats.Count(c => c.RoomId == callback.Sender) == 0)
                    {
                        var c = Join(callback.Sender);

                        if (OnPrivateEnter != null)
                            OnPrivateEnter(c);
                    }
                });

                msg.Handle<SteamFriends.FriendsListCallback>(callback =>
                {
                    foreach (var friend in callback.FriendList)
                    {
                        var f = friend;
                        if (friend.Relationship == EFriendRelationship.RequestRecipient && OnFriendRequest != null)
                            OnFriendRequest(f.SteamID);
                    }
                });

                msg.Handle<SteamFriends.ChatInviteCallback>(callback =>
                {
                    if (OnChatInvite != null)
                        OnChatInvite(callback.ChatRoomID, callback.PatronID);
                });

                msg.Handle<ClanNameCallback>(callback =>
                {
                    ClanNames[callback.ClanID] = callback.Name;
                });

                foreach (var c in chats)
                {
                    c.Handle(msg);
                }

                chats.RemoveAll(c => c.Left);
            }
        }

        public static SteamChat Join(SteamID roomId)
        {
            if (Status != ConnectionStatus.Connected)
            {
                Logger.Warn("Attempt to Join chat when not connected");
                return null;
            }

            Friends.RequestFriendInfo(roomId);

            if (roomId.IsClanAccount)
            {
                Friends.RequestFriendInfo(roomId, EClientPersonaStateFlag.ClanInfo | EClientPersonaStateFlag.ClanTag | EClientPersonaStateFlag.PlayerName);
                roomId = SteamUtil.ChatFromClan(roomId);
            }

            if (chats.Any(c => c.RoomId == roomId))
                return chats.Find(c => c.RoomId == roomId);

            var chat = new SteamChat(roomId);
            Friends.JoinChat(roomId);
            chats.Add(chat);

            return chat;
        }

        public static ReadOnlyCollection<SteamID> GetFriends()
        {
            var res = new List<SteamID>();

            for (var i = 0; i < Friends.GetFriendCount(); i++)
            {
                res.Add(Friends.GetFriendByIndex(i));
            }

            return res.AsReadOnly();
        }

        public static ReadOnlyCollection<SteamID> GetClans()
        {
            var res = new List<SteamID>();

            for (var i = 0; i < Friends.GetClanCount(); i++)
            {
                var c = Friends.GetClanByIndex(i);
                if (Friends.GetClanRelationship(c) == EClanRelationship.Member)
                    res.Add(c);
            }

            return res.AsReadOnly();
        } 

        public static void Announce(string message, Predicate<SteamChat> filter = null)
        {
            foreach (var c in chats)
            {
                if (filter != null && !filter(c))
                    continue;

                c.Send(message);
            }
        }

        public static void SetPlaying(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
            {
                var msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayedWithDataBlob);
                msg.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed { game_id = 0, game_extra_info = "" });
                Client.Send(msg);
            }
            else
            {
                var msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayedWithDataBlob);
                msg.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed { game_id = 9759487592989982720, game_extra_info = gameName });
                Client.Send(msg);
            }
        }

        public static string GetClanName(SteamID id)
        {
            string name;
            return ClanNames.TryGetValue(id, out name) ? name : "[unknown]";
        }
    }
}
