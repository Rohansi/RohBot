using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamMobile;
using SteamMobile.Packets;
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
