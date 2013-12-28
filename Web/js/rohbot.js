
var storage = window.rohStore;

var roomName;
var requestedHistory;
var oldestMessage;

var rohbot = null;
function initializeRohBot() {
	var serverUri = "wss://fpp.literallybrian.com/ws/";
	if (window.location.search == "?noproxy")
		serverUri = "ws://fpp.literallybrian.com:12000/";
	
	rohbot = new RohBot(serverUri); 
	rohbot.onConnected = function() {
		oldestMessage = 0xFFFFFFFF;
		requestedHistory = false;
		
		var room = window.location.hash.substring(1);
		if (storage && storage.getItem("password") !== null) {
			rohbot.login(storage.getItem("name"), storage.getItem("password"), null, room);
		} else if (storage && storage.getItem("tokens") !== null) {
			rohbot.login(storage.getItem("name"), null, storage.getItem("tokens"), room);
		} else {
			rohbot.login("guest", null, null, room);
		}
	};
	
	rohbot.onLogin = function(data) {
		if (storage) {
			storage.setItem("name", data.Name);
			storage.setItem("tokens", data.Tokens);
		}
			
		if (data.Success) {
			$("#header").hide();
			$("#messageBox").removeAttr("disabled");
			$("#messageBox").val("");
			$("#password").val("");
		} else {
			$("#header").show();
			$("#messageBox").attr("disabled","disabled");
			$("#messageBox").val("Guests can not speak.");
		}
		
		$("#chat").scrollTop($("#chat")[0].scrollHeight);
	};
	
	rohbot.onChatHistory = function(data) {
		window.chatMgr.addChatHistory( data.Lines, data.Requested );

		if (data.Requested)
			requestedHistory = false;

		roomName = data.Name;
		document.title = roomName;
		$("#title").text(roomName);
		oldestMessage = data.OldestLine;
	};

	var p = document.createElement('p');
	function htmlDecode( str )
	{
		p.innerHTML = str;
		return p.textContent;
	}
	
	rohbot.onMessage = function(line) {
		if (line.Type == "chat" && line.Sender != rohbot.name) {
			if (notifications.checkMessage( line.Content )) {
				notifications.doNotification(
					roomName,
					htmlDecode(line.Sender) + ": " + htmlDecode(line.Content)
				);
			}
		}
		
		window.chatMgr.addLine(line, false);
	};
	
	rohbot.onSysMessage = function(line) {
		line.Type = "state";
		window.chatMgr.addLine(line, false);
	};
	function userListFilter(user)
	{
		return user.Name !== 'Guest';
	}
	function userListMap(user)
	{
		// People w/o avatars, use the ? avatar
		if (user.Avatar == "0000000000000000000000000000000000000000")
			user.Avatar = "fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb";
		if (user.Web)
			user.Avatar = false;
		else
			user.AvatarFolder = user.Avatar.substring(0, 2);
		if ( ! user.Playing ) // Explicit falsy = false
			user.Playing = false;
		user.Color = user.Playing ? "ingame" : ( user.Web ? "web" : "" );
		return user;
	}
	rohbot.onUserList = function(users) {
		window.chatMgr.statusMessage('In this room:');

		var html = templates.users.render({
			Users: users.filter(userListFilter).map(userListMap)
		});
		window.chatMgr.addHtml(html, false);
	};
	
	rohbot.connect();
}

$(document).ready(function() {
	initializeRohBot();

	window.chatMgr = new ChatManager( rohbot );
	window.notifications = new NotificationCenter();

	$("#password").keydown(function(e) {
		if (e.keyCode == 13) {
			$("#password").blur().focus();
			$("#loginButton").click();
			return false;
		}
	});
	
	$("#loginButton").click(function() {
		rohbot.login($("#username").val(), $("#password").val(), null);
	});
	
	$("#registerButton").click(function() {
		rohbot.register($("#username").val(), $("#password").val());
	});
	
	$("#chat").scroll(function() {
		if ($("#chat").scrollTop() == 0 && !requestedHistory) {
			rohbot.requestHistory(oldestMessage);
			requestedHistory = true;
		}
	});
	
	$(window).resize(function() {
		$("#chat").scrollTop($("#chat")[0].scrollHeight);
	});
});
