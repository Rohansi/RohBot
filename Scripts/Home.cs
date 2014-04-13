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
			"hello world", "hello"
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

                SendMessage(connection, "to join a room click on its name below");
                SendMessage(connection, "");

                var rooms = Program.RoomManager.List.Where(r => !r.IsHidden);
                foreach (var room in rooms)
                {
                    var shortName = room.RoomInfo.ShortName;
                    var name = room.RoomInfo.Name;
                    var notes = new List<string>();

                    name = JsLink("join('" + shortName +"')", name);
                    
                    if (room is SteamRoom)
                        notes.Add(Link("http://steamcommunity.com/gid/" + room.RoomInfo["SteamId"], "steam"));

                    if (room.IsWhitelisted)
                        notes.Add("whitelisted");

                    if (room.IsPrivate)
                        notes.Add("private");

                    SendMessage(connection, string.Format("{1}{2}{3}",
                        shortName,
                        name,
                        notes.Count == 0 ? "" : " — ",
                        string.Join(", ", notes)));
                }

                SendMessage(connection, "");
                SendMessage(connection, "need help with commands? " + Link("https://github.com/Rohansi/SteamMobile#commands", "read this"));
                SendMessage(connection, "want me in your group? " + Link("http://steamcommunity.com/id/rohans/", "talk to this guy"));
            }
        }
    }

    private void SendMessage(Connection connection, string message)
    {
        connection.Send(new Message
        {
            Line = new StateLine
            {
                Date = Util.GetCurrentUnixTimestamp(),
                Chat = "home",
                Content = message,
                State = "Client"
            }
        });
    }

    private static string Link(string target, string caption)
    {
        return string.Format("<a href=\"{0}\" target=\"_blank\">{1}</a>", target, Util.HtmlEncode(caption));
    }
    
    private static string JsLink(string target, string caption)
    {
        return string.Format("<a onclick=\"{0}\">{1}</a>", target, Util.HtmlEncode(caption));
    }

    public bool OnSendLine(HistoryLine line) { return true; }
    public bool OnSendMessage(Connection connection, string message) { return true; }
}
