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
	private Queue<Session> _sessions;
	private List<string> _greetings;
	
	public void Initialize(ScriptHost host)
	{
		_host = host;
		_sessions = new Queue<Session>();

		_greetings = new List<string>()
		{
			"hi!", "sup", "hi mom", "hi how are you", "beep", "boop", "hi im dog",
			"hello world"
		};
	}
	
	public bool OnSendHistory(Session session)
	{
		lock (_sessions)
			_sessions.Enqueue(session);

		session.Send(new ChatHistory
		{
			Name = "RohBot Home",
			ShortName = "home",
			Requested = false,
			Lines = new List<HistoryLine>()
		});
		
		return false;
	}
	
	public void Update(float deltaTime)
	{
		lock (_sessions)
		{
			while (_sessions.Count > 0)
			{
				var session = _sessions.Dequeue();

				SendMessage(session, _greetings[new Random().Next(_greetings.Count)]);

				if (session.Account == null)
					SendMessage(session, "you need to create an account to send messages");

				SendMessage(session, "to switch between rooms use the '/room' command");
				SendMessage(session, "you can also set your default room with it so you never see this again");
				SendMessage(session, "use '/room list' to get a list of rooms you can join");
				SendMessage(session, "you will need to use the name in brackets to switch");
				SendMessage(session, "----------");
				SendMessage(session, "Want me in your group? Talk to this guy: http://steamcommunity.com/id/rohans/");
				SendMessage(session, "More help can be found here: https://github.com/Rohansi/SteamMobile#commands");
			}
		}
	}
	
	private void SendMessage(Session session, string message)
	{
		session.Send(new Message
		{
			Line = new ChatLine(Util.GetCurrentUnixTimestamp(), "", "Steam", "~", "0", "", message, false)
		});
	}
	
	public bool OnSendLine(HistoryLine line) { return true; }
	public bool OnSendMessage(Session session, string message) { return true; }
}
