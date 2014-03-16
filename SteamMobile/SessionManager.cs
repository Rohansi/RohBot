using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SuperWebSocket;

namespace SteamMobile
{
    public class SessionManager
    {
        private class Server : WebSocketServer<Connection>
        {
            
        }
       
        private Server _server;
        private Dictionary<string, Session> _sessions;
        private Stopwatch _timer;

        public SessionManager()
        {
            _sessions = new Dictionary<string, Session>();
            _timer = Stopwatch.StartNew();

            _server = new Server();
            _server.Setup("0.0.0.0", 12000);
            _server.Start();

            _server.NewMessageReceived += OnReceive;
        }

        public void Broadcast(Packet packet, Func<Session, bool> filter = null)
        {
            var packetStr = Packet.WriteToMessage(packet);

            foreach (var session in _sessions.Values)
            {
                if (filter == null || filter(session))
                {
                    session.Send(packetStr);
                }
            }
        }

        public void Update()
        {
            lock (_sessions)
            {
                // TODO: can replace this to provide disconnect messages
                _sessions.RemoveAll(kv => kv.Value.TimeWithoutConnections >= 20);

                foreach (var session in _sessions.Values)
                {
                    session.Update((float)_timer.Elapsed.TotalSeconds);
                }

                _timer.Restart();
            }
        }

        public List<Session> List
        {
            get
            {
                lock (_sessions)
                    return _sessions.Values.ToList();
            }
        }

        public Session GetOrCreate(Account account)
        {
            lock (_sessions)
            {
                Session result;
                if (!_sessions.TryGetValue(account.Name, out result))
                {
                    result = new Session(account);
                    _sessions.Add(account.Name, result);
                }

                return result;
            }
        }

        public Session Get(string name)
        {
            name = (name ?? "").ToLower();

            lock (_sessions)
            {
                Session result;
                _sessions.TryGetValue(name, out result);
                return result;
            }
        }

        private void OnReceive(Connection connection, string message)
        {
            try
            {
                Packet.ReadFromMessage(message).Handle(connection);
            }
            catch (Exception e)
            {
                Program.Logger.Error(string.Format("Bad packet from {0}: {1}", connection.Address, message), e);
            }
        }
    }
}
