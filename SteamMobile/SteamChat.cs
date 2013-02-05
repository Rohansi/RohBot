using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SteamKit2;

namespace SteamMobile
{
    public enum UserLeaveReason
    {
        Left, Disconnected, Kicked, Banned
    }

    class SteamChat
    {

        public delegate void ChatEnterEvent(SteamChat source);
        public delegate void ChatLeaveEvent(SteamChat source);
        public delegate void MessageEvent(SteamChat source, SteamID messageSender, string message);
        public delegate void UserEnterEvent(SteamChat source, SteamID user);
        public delegate void UserLeaveEvent(SteamChat source, SteamID user, UserLeaveReason reason);

        public SteamID RoomId { get; protected set; }

        public bool Left { get; protected set; }
        public EChatRoomEnterResponse? Response { get; protected set; }

        public ChatEnterEvent OnEnter = null;
        public ChatLeaveEvent OnLeave = null;
        public MessageEvent OnMessage = null;
        public UserEnterEvent OnUserEnter = null;
        public UserLeaveEvent OnUserLeave = null;

        public string Title
        {
            get
            {
                if (RoomId.IsIndividualAccount)
                    return Steam.Friends.GetFriendPersonaName(RoomId);
                if (RoomId.AccountInstance == 3260)
                    return string.Join(" + ", Members.Select(id => Steam.Friends.GetFriendPersonaName(id)));
                var clan = SteamUtil.ClanFromChat(RoomId);
                if (clan.IsValid)
                    return Steam.GetClanName(clan);
                return "[unknown]";
            }
        }

        private readonly List<SteamID> members = new List<SteamID>();
        public ReadOnlyCollection<SteamID> Members
        {
            get { return members.AsReadOnly(); }
        }

        private readonly LinkedList<Tuple<string, string>> history = new LinkedList<Tuple<string, string>>();
        public ReadOnlyCollection<Tuple<string, string>> History
        {
            get { return new ReadOnlyCollection<Tuple<string, string>>(history.ToList()); }
        } 

        public SteamChat(SteamID roomId)
        {
            RoomId = roomId;
            Left = false;
            Response = null;
        }

        public void Send(string message)
        {
            if (Left || (RoomId.IsChatAccount && Response != EChatRoomEnterResponse.Success))
                return;

            if (RoomId.IsChatAccount)
                Steam.Friends.SendChatRoomMessage(RoomId, EChatEntryType.ChatMsg, message);
            else
                Steam.Friends.SendChatMessage(RoomId, EChatEntryType.ChatMsg, message);

            AddHistory(Steam.Friends.GetPersonaName(), message);
        }

        public void Leave()
        {
            if (Response != null && OnLeave != null)
                OnLeave(this);

            Left = true;
            Steam.Friends.LeaveChat(RoomId);
        }

        public void Handle(CallbackMsg msg)
        {
            msg.Handle<SteamFriends.ChatMsgCallback>(callback =>
            {
                if (callback.ChatMsgType != EChatEntryType.ChatMsg || callback.ChatRoomID != RoomId)
                    return;

                if (OnMessage != null)
                    OnMessage(this, callback.ChatterID, callback.Message);

                AddHistory(Steam.Friends.GetFriendPersonaName(callback.ChatterID), callback.Message);
            });

            msg.Handle<SteamFriends.FriendMsgCallback>(callback =>
            {
                if (callback.EntryType != EChatEntryType.ChatMsg || callback.Sender != RoomId)
                    return;

                if (OnMessage != null)
                    OnMessage(this, callback.Sender, callback.Message);

                 AddHistory(Steam.Friends.GetFriendPersonaName(callback.Sender), callback.Message);
            });

            msg.Handle<SteamFriends.ChatMemberInfoCallback>(callback =>
            {
                if (callback.ChatRoomID != RoomId)
                    return;

                var state = callback.StateChangeInfo.StateChange;
                switch (state)
                {
                    case EChatMemberStateChange.Entered:
                        if (OnUserEnter != null)
                            OnUserEnter(this, callback.StateChangeInfo.ChatterActedOn);

                        members.Add(callback.StateChangeInfo.ChatterActedOn);

                        break;

                    case EChatMemberStateChange.Left:
                    case EChatMemberStateChange.Disconnected:
                        if (OnUserLeave != null)
                            OnUserLeave(this, callback.StateChangeInfo.ChatterActedOn, 
                                state == EChatMemberStateChange.Left ? UserLeaveReason.Left : UserLeaveReason.Disconnected);

                        members.Remove(callback.StateChangeInfo.ChatterActedOn);

                        break;

                    case EChatMemberStateChange.Kicked:
                    case EChatMemberStateChange.Banned:
                        if (callback.StateChangeInfo.ChatterActedOn == Steam.User.SteamID)
                        {
                            Leave();
                        }
                        else
                        {
                            if (OnUserLeave != null)
                                OnUserLeave(this, callback.StateChangeInfo.ChatterActedOn,
                                    state == EChatMemberStateChange.Kicked ? UserLeaveReason.Kicked : UserLeaveReason.Banned);
                        }

                        members.Remove(callback.StateChangeInfo.ChatterActedOn);

                        break;
                }
            });

            msg.Handle<SteamFriends.ChatEnterCallback>(callback =>
            {
                if (callback.ChatID != RoomId)
                    return;

                if (callback.EnterResponse != EChatRoomEnterResponse.Success)
                {
                    Console.WriteLine("Failed to join chat: " + callback.EnterResponse);
                    Leave();
                    return;
                }

                Response = callback.EnterResponse;

                if (OnEnter != null)
                    OnEnter(this);
            });

            msg.Handle<SteamFriends.ChatActionResultCallback>(callback =>
            {
                if (callback.ChatRoomID != RoomId)
                    return;

                if (callback.Result != EChatActionResult.Success)
                    Console.WriteLine("Chat action failed: " + callback.Result);
            });

            msg.Handle<SteamFriends.PersonaStateCallback>(callback =>
            {
                if (callback.SourceSteamID != RoomId || Response != null)
                    return;
                members.Add(callback.FriendID);
            });
        }

        public void AddHistory(string sender, string message)
        {
            if (history.Count >= 50)
                history.RemoveFirst();
            history.AddLast(Tuple.Create(sender, message));
        }
    }
}
