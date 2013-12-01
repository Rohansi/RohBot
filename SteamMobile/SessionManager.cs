using System;
using System.Collections.Generic;
using System.Linq;
using SuperWebSocket;

namespace SteamMobile
{
    public class SessionManager
    {
        private class Server : WebSocketServer<Session>
        {
            
        }
       
        private Server _server;
         
        public SessionManager()
        {
            _server = new Server();
            _server.Setup("0.0.0.0", 12000);
            _server.Start();

            _server.NewMessageReceived += OnReceive;
        }

        public void Broadcast(Packet packet, Func<Session, bool> filter = null)
        {
            var packetStr = Packet.WriteToMessage(packet);

            foreach (var session in _server.GetAllSessions())
            {
                if (filter == null || filter(session))
                {
                    session.Send(packetStr);
                }
            }
        }

        public void Update()
        {
            
        }

        public List<Session> List
        {
            get
            {
                return _server.GetAllSessions().ToList();
            }
        }

        public Session Get(string name)
        {
            name = name.ToLower();
            return _server.GetAllSessions().FirstOrDefault(s => s.Account != null && s.Account.Name.ToLower() == name);
        }

        private void OnReceive(Session session, string message)
        {
            try
            {
                Packet.ReadFromMessage(message).Handle(session);
            }
            catch (Exception e)
            {
                Program.Logger.Error(string.Format("Bad packet from {0}: {1}", session.Address, message), e);
            }
        }
    }
}
