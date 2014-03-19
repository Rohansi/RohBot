using System;
using System.Collections.Generic;
using System.Linq;
using SteamMobile;
using SteamMobile.Packets;
using SteamMobile.Rooms;
using SteamMobile.Rooms.Script;

public class Script : IScript
{
    private ScriptHost _host;
    private Queue<Connection> _connections;
    private List<string> _greetings;

    public void Initialize(ScriptHost host)
    {
        _host = host;
        _connections = new Queue<Connection>();

        _greetings = new List<string>()
		{
			"hi!", "sup", "hi mom", "hi how are you", "beep", "boop", "hi im dog",
			"hello world"
		};
    }

    public bool OnSendHistory(Connection connection)
    {
        lock (_connections)
            _connections.Enqueue(connection);

        connection.Send(new ChatHistory
        {
            ShortName = "home",
            Requested = false,
            Lines = new List<HistoryLine>()
        });

        return false;
    }

    public void Update(float deltaTime)
    {
        lock (_connections)
        {
            while (_connections.Count > 0)
            {
                var connection = _connections.Dequeue();

                SendMessage(connection, _greetings[new Random().Next(_greetings.Count)]);

                if (connection.Session == null)
                    SendMessage(connection, "you need to create an account to send messages");

                SendMessage(connection, "to join other rooms use the '/join' command");
                SendMessage(connection, "more help can be found here: https://github.com/Rohansi/SteamMobile#commands");
                SendMessage(connection, "");
                SendMessage(connection, "here are some of the rooms you can join:");

                var rooms = Program.RoomManager.List.Where(r => !r.IsHidden);
                foreach (var room in rooms)
                {
                    var notes = new List<string>();

                    if (room is SteamRoom)
                        notes.Add(string.Format("http://steamcommunity.com/gid/{0}", room.RoomInfo["SteamId"]));

                    if (room.IsWhitelisted)
                        notes.Add("whitelisted");

                    if (room.IsPrivate)
                        notes.Add("private");

                    SendMessage(connection, string.Format("{0} -> {1}{2}{3}",
                        room.RoomInfo.ShortName,
                        room.RoomInfo.Name,
                        notes.Count == 0 ? "" : " - ",
                        string.Join(", ", notes)));
                }

                SendMessage(connection, "");
                SendMessage(connection, "want me in your group? talk to this guy: http://steamcommunity.com/id/rohans/");
            }
        }
    }

    private void SendMessage(Connection connection, string message)
    {
        connection.Send(new Message
        {
            Line = new ChatLine(Util.GetCurrentUnixTimestamp(), "home", "Steam", "~", "0", "", message, false)
        });
    }

    public bool OnSendLine(HistoryLine line) { return true; }
    public bool OnSendMessage(Connection connection, string message) { return true; }
}
