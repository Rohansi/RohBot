using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace SteamMobile
{
    public class AccountInfo
    {
        public ObjectId Id;
        public string SteamId;
        public string Name;
        public long LastNameChange;
        public string DefaultRoom;

        public class Comparer : IEqualityComparer<AccountInfo>
        {
            public bool Equals(AccountInfo x, AccountInfo y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;
                return x.SteamId == y.SteamId;
            }

            public int GetHashCode(AccountInfo obj)
            {
                if (ReferenceEquals(obj, null))
                    return 0;
                return obj.SteamId.GetHashCode();
            }
        }
    }

    public class Session
    {
        public AccountInfo AccountInfo;
        public string Room;
        
        public readonly IWebSocketConnection Socket;
        public readonly string Address;

        public Session(IWebSocketConnection socket)
        {
            Socket = socket;
            Address = socket.ConnectionInfo.ClientIpAddress;

            var sourceTokens = socket.ConnectionInfo.Cookies.Values.ToList();
            var token = Database.LoginTokens.AsQueryable()
                                            .Where(r => r.Address == socket.ConnectionInfo.ClientIpAddress)
                                            .Where(r => sourceTokens.Contains(r.Token))
                                            .OrderByDescending(r => r.Created)
                                            .FirstOrDefault();
            if (token == null)
            {
                AccountInfo = new AccountInfo
                {
                    SteamId = "0",
                    Name = "Guest",
                    DefaultRoom = Program.Settings.DefaultRoom
                };
            }
            else
            {
                AccountInfo = Database.AccountInfo.AsQueryable().FirstOrDefault(r => r.SteamId == token.SteamId);

                if (AccountInfo == null)
                {
                    AccountInfo = new AccountInfo
                    {
                        SteamId = token.SteamId,
                        Name = null,
                        DefaultRoom = Program.Settings.DefaultRoom
                    };

                    Database.AccountInfo.Save(AccountInfo);
                }
            }

            Room = AccountInfo.DefaultRoom;

            var ready = new Packets.Ready();
            ready.SteamId = AccountInfo.SteamId;
            Send(ready);

            var room = Program.RoomManager.Get(Room);
            if (room != null)
                room.SendHistory(this);
        }

        public void Send(Packet packet)
        {
            Socket.Send(Packet.WriteToMessage(packet));
        }

        public void SendRaw(string str)
        {
            Socket.Send(str);
        }
    }
}
