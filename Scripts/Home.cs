using System;
using System.Collections.Generic;
using System.Linq;
using RohBot;
using RohBot.Packets;
using RohBot.Rooms;
using RohBot.Rooms.Steam;
using RohBot.Rooms.Script;

public class Script : IScript
{
    private ScriptHost _host;
    private List<string> _greetings;

    public void Initialize(ScriptHost host)
    {
        _host = host;

        _greetings = new List<string>()
        {
            "hi!", "sup", "hi mom", "hi how are you", "beep", "boop", "hi im dog",
            "hello world", "hello"
        };
    }

    public bool OnSendHistory(Connection connection)
    {
        var lines = new List<HistoryLine>();
        
        lines.Add(Message(_greetings[new Random().Next(_greetings.Count)]));

        if (connection.Session == null)
        {
            lines.Add(Message("this is a web interface for steam group chats (try it on your phone!)"));
            lines.Add(Message("you will need to create an account to use it"));
        }
        else
        {
            lines.Add(Message("to join a room click on its name below"));
            lines.Add(Message(""));

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

                lines.Add(Message(string.Format("{0}{1}{2}",
                    name,
                    notes.Count == 0 ? "" : " -- ",
                    string.Join(", ", notes))));
            }
        }

        lines.Add(Message(""));
        lines.Add(Message("need help with commands? " + Link("https://github.com/Rohansi/RohBot#commands", "read this")));
        lines.Add(Message("want me in your group? " + Link("http://steamcommunity.com/id/rohans/", "talk to this guy")));
    
        connection.Send(new ChatHistory
        {
            ShortName = "home",
            Requested = false,
            Lines = lines
        });

        return false;
    }

    private HistoryLine Message(string message)
    {
        return new StateLine
        {
            Date = Util.GetCurrentTimestamp(),
            Chat = "home",
            Content = message,
            State = "Client"
        };
    }

    private static string Link(string target, string caption)
    {
        return string.Format("<a href=\"{0}\" target=\"_blank\">{1}</a>", target, Util.HtmlEncode(caption));
    }
    
    private static string JsLink(string target, string caption)
    {
        return string.Format("<a onclick=\"{0}\">{1}</a>", target, Util.HtmlEncode(caption));
    }
    
    public void Update(float deltaTime) { }
    public bool OnSendLine(HistoryLine line) { return true; }
    public bool OnSendMessage(Connection connection, string message) { return true; }
}
