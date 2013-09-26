using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;

namespace SteamMobile
{
    public class SessionManager
    {
        private WebSocketServer _server;
        private Dictionary<Guid, Session> _sessions;
         
        public SessionManager()
        {
            _sessions = new Dictionary<Guid, Session>();

            _server = new WebSocketServer("ws://0.0.0.0:12000/");
            _server.Start(socket =>
            {
                socket.OnOpen = () => OnConnected(socket);
                socket.OnClose = () => OnDisconnect(socket);
                socket.OnMessage = message => OnReceive(socket, message);
            });
        }

        public void Broadcast(Packet packet, Func<Session, bool> filter = null)
        {
            var packetStr = Packet.WriteToMessage(packet);

            lock (_sessions)
            {
                foreach (var session in _sessions.Values)
                {
                    if (filter == null || filter(session))
                    {
                        session.SendRaw(packetStr);
                    }
                }
            }
        }

        public void Update()
        {
            lock (_sessions)
                _sessions.RemoveAll(kv => !kv.Value.Socket.IsAvailable);
        }

        public List<Session> List
        {
            get
            {
                lock (_sessions)
                    return _sessions.Values.ToList();
            }
        } 

        private void OnConnected(IWebSocketConnection socket)
        {
            lock (_sessions)
                _sessions.Add(socket.ConnectionInfo.Id, new Session(socket));
        }

        private void OnDisconnect(IWebSocketConnection socket)
        {
            lock (_sessions)
                _sessions.Remove(socket.ConnectionInfo.Id);
        }

        private void OnReceive(IWebSocketConnection socket, string message)
        {
            Session session;

            try
            {
                lock (_sessions)
                    session = _sessions[socket.ConnectionInfo.Id];
            }
            catch
            {
                socket.Close();
                return;
            }

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
