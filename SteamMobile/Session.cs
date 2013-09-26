using System.Collections.Generic;
using Fleck;

namespace SteamMobile
{
    public class Session
    {
        public Account Account;
        public string Room;
        
        public readonly IWebSocketConnection Socket;
        public readonly string Address;

        public Session(IWebSocketConnection socket)
        {
            Account = null;
            Room = "";

            Socket = socket;
            Address = socket.ConnectionInfo.ClientIpAddress;
        }

        public void Login(string username, string password, List<string> tokens)
        {
            // TODO: login

            // TODO: send AuthenticateResponse

            /*var room = Program.RoomManager.Get(Room);
            if (room != null)
                room.SendHistory(this);*/
        }

        public void Register(string username, string password)
        {
            // TODO: register

            Login(username, password, new List<string>());
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
