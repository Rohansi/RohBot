using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Rohmote;

namespace RohBot.Rooms.Remote
{
    public class RemoteRoom : Room
    {
        public override string CommandPrefix { get { return "remote_"; } }

        public Dictionary<string, string> Commands;

        private RpcClient _client;
        private bool _hasConnected;
        private bool _ready;

        public RemoteRoom(RoomInfo roomInfo)
            : base(roomInfo)
        {
            _hasConnected = false;
        }

        public override void Update()
        {
            if (!IsActive)
            {
                if (_client != null)
                    _client.Dispose();

                _client = null;
                return;
            }

            if (_client != null)
                return;

            var ip = RoomInfo["Address"];
            var port = int.Parse(RoomInfo["Port"]);

            _client = new RpcClient(ip, port);
            _client.CallTimeOut = TimeSpan.FromSeconds(5);
            _client.DetailedErrorMessages = true;

            _client.Connected += () =>
            {
                _hasConnected = true;
                _ready = false;
                Commands = null;
            };

            _client.Disconnected += () =>
            {
                _client.Dispose();
                _client = null;
                _ready = false;
                Commands = null;

                if (_hasConnected)
                    SendStatus("Lost connection to Remote.");
                
                _hasConnected = false;
            };

            _client.Error += exception => Program.Logger.ErrorFormat("RpcClient Error: {0}", exception);

            _client.On("CommandList", (Dictionary<string, string> commands) =>
            {
                if (!_ready)
                    SendStatus("Connected to Remote.");

                Commands = commands;
                _ready = true;
                return 0;
            });

            _client.On("AccessRoomInfo", (string key) => RoomInfo[key]);

            _client.On("UserList", () =>
            {
                var sessions = Program.SessionManager.List;
                var accounts = sessions.Where(s => s.IsInRoom(RoomInfo.ShortName))
                                   .Select(s => s.Account)
                                   .Distinct(new Account.Comparer());

                return accounts.Select(a => Tuple.Create(a.Id, a.Name)).ToList();
            });

            _client.On("BaseSendLine", (JToken obj) =>
            {
                var line = JTokenToLine(obj);
                base.SendLine(line);
                return 0;
            });

            _client.On("Send", (string str) =>
            {
                Send(str);
                return 0;
            });
        }

        public override void SendLine(HistoryLine line)
        {
            try
            {
                if (_ready)
                    _client.Call<HistoryLine, int>("SendLine", line).Wait();
            }
            catch (Exception e)
            {
                Program.Logger.ErrorFormat("Remote SendLine: {0}", e);
            }
        }

        public override IEnumerable<Session> SendLineFilter(HistoryLine line, IEnumerable<Session> sessions)
        {
            if (!_ready)
                return sessions;

            var sessionsList = sessions.ToList();

            try
            {
                Func<Task<IEnumerable<Session>>> parallel = async () =>
                {
                    var tasks = sessionsList.Select(async session =>
                    {
                        var account = session.Account;
                        var accountData = Tuple.Create(account.Id, account.Name);
                        return await _client.Call<HistoryLine, Tuple<long, string>, bool>("SendLineFilter", line, accountData);
                    }).ToList();

                    await Task.WhenAll(tasks);

                    return tasks.Select(t => t.Result)
                                .Zip(sessionsList, (include, session) => include ? session : null)
                                .Where(s => s != null);
                };

                return parallel().Result;
            }
            catch (Exception e)
            {
                Program.Logger.ErrorFormat("Remote SendLineFilter: {0}", e);
                return sessionsList;
            }
        }

        public override List<HistoryLine> GetHistoryLines(Connection connection)
        {
            try
            {
                if (_ready)
                {
                    var account = connection.Session.Account;
                    var accountData = Tuple.Create(account.Id, account.Name);

                    var linesObj = _client.Call<Tuple<long, string>, List<JToken>>("GetHistoryLines", accountData).Result;
                    if (linesObj == null)
                        return base.GetHistoryLines(connection);

                    var lines = linesObj.Select(JTokenToLine).ToList();
                    return lines;
                }
            }
            catch (Exception e)
            {
                Program.Logger.ErrorFormat("Remote GetHistoryLines: {0}", e);
            }

            return base.GetHistoryLines(connection);
        }

        public void CallCommand(CommandTarget target, string type, string[] parameters)
        {
            try
            {
                if (_ready)
                {
                    var account = target.Connection.Session.Account;
                    if (account == null)
                        return;

                    var accountData = Tuple.Create(account.Id, account.Name);
                    var packets = _client.Call<Tuple<long, string>, string, string[], List<string>>("Command", accountData, type, parameters).Result;

                    foreach (var packet in packets)
                    {
                        target.Connection.Send(packet);
                    }
                }
            }
            catch (Exception e)
            {
                Program.Logger.ErrorFormat("Remote CallCommand: {0}", e);
            }
        }

        private void SendStatus(string msg)
        {
            base.SendLine(new ChatLine(Util.GetCurrentTimestamp(), RoomInfo.ShortName, "Steam", Program.Settings.PersonaName, "0", "", msg, false));
        }

        private static HistoryLine JTokenToLine(JToken token)
        {
            var type = token["Type"].ToObject<string>();

            switch (type)
            {
                case "chat":
                    return token.ToObject<ChatLine>();
                case "state":
                    return token.ToObject<StateLine>();
                default:
                    throw new NotSupportedException("Cannot convert HistoryLine type: " + type);
            }
        }
    }
}
