using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace RohBot
{
    public class SessionManager
    {
        private WebSocketServer<Connection> _server;
        private ConcurrentDictionary<string, Session> _sessions;
        private Stopwatch _timer;

        public SessionManager()
        {
            _sessions = new ConcurrentDictionary<string, Session>();
            _timer = Stopwatch.StartNew();
        }

        public void Start()
        {
            _server = new WebSocketServer<Connection>(new IPEndPoint(IPAddress.Any, 12000));
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

        public void Send(Packet packet, IEnumerable<Session> sessions)
        {
            var packetStr = Packet.WriteToMessage(packet);

            foreach (var session in sessions)
            {
                session.Send(packetStr);
            }
        }

        public void Update()
        {
            var emptySessions = _sessions.Where(kv => !kv.Value.IsActive);
            foreach (var empty in emptySessions)
            {
                Session removedSession;

                if (_sessions.TryRemove(empty.Key, out removedSession))
                {
                    var rooms = Program.RoomManager.List.Where(r => removedSession.IsInRoom(r.RoomInfo.ShortName));
                    foreach (var r in rooms)
                    {
                        r.SessionDisconnect(removedSession);
                    }
                }
            }

            foreach (var session in _sessions)
            {
                session.Value.Update((float)_timer.Elapsed.TotalSeconds);
            }

            _timer.Restart();
        }

        public ICollection<Session> List => _sessions.Values;

        public Session GetOrCreate(Account account)
        {
            return _sessions.GetOrAdd(account.Name, k => new Session(account));
        }

        public Session Get(string name)
        {
            Session result;
            _sessions.TryGetValue(name, out result);
            return result;
        }

        public void Ping()
        {
            var ping = new Packets.Ping();
            var pingStr = Packet.WriteToMessage(ping);

            try
            {
                foreach (var client in _server.Clients)
                {
                    client.Send(pingStr);
                }
            }
            catch (Exception e)
            {
                Program.Logger.Warn("ping failed", e);
            }
        }

        public void Close(Connection connection)
        {
            connection.Close();
        }
    }
}
