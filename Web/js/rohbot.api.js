RohBot = function(server) {
	var _this = this;
	
	var socket = null;
	var firstConnect = true;
	var hasConnected = false;
	
	_this.name = null;
	
	_this.onConnected = null;
	_this.onDisconnected = null;
	_this.onLogin = null;
	_this.onChatHistory = null;
	_this.onSysMessage = null;
	_this.onMessage = null;
	_this.onUserList = null;
	
	var getCurrentTime = function() {
		return new Date().getTime() / 1000;
	};
	
	var closeSocket = function() {
		if (socket !== null) {
			socket.close();
			socket.onopen = null;
			socket.onclose = null;
			socket.onerror = null;
			socket.onmessage = null;
			socket = null;
		}
	};
	
	setInterval(function () {
		if (socket !== null) {
			send({ Type: "ping" });
		} else {
			_this.connect();
		}
	}, 2500);
	
	_this.connect = function() {
		if (firstConnect) {
			if (_this.onSysMessage != null)
				_this.onSysMessage({Date: getCurrentTime(), Content: "Connecting to RohBot..."});
			firstConnect = false;
		}
		
		closeSocket();
		socket = new WebSocket(server);
		
		socket.onopen = function (event) {
			if (_this.onSysMessage != null)
				_this.onSysMessage({Date: getCurrentTime(), Content: "Connected to RohBot!"});
			if (_this.onConnected != null)
				_this.onConnected();
			hasConnected = true;
		};
		
		socket.onclose = socket.onerror = function (event) {
			closeSocket();
			
			if (hasConnected) {
				if (_this.onSysMessage != null)
					_this.onSysMessage({Date: getCurrentTime(), Content: "Lost connection to RohBot. Reconnecting..."});
				hasConnected = false;
			}
			
			if (_this.onDisconnected != null)
				_this.onDisconnected();
		};
		
		socket.onmessage = function (event) {
			var data = JSON.parse(event.data);
			
			switch (data.Type)
			{
				case "authResponse": {
					_this.name = data.Name;
					if (_this.onLogin != null)
						_this.onLogin(data);
					break;
				}
				
				case "chatHistory": {
					if (_this.onChatHistory != null)
						_this.onChatHistory(data);
					break;
				}
				
				case "message": {
					if (_this.onMessage != null)
						_this.onMessage(data.Line);
					break;
				}
				
				case "sysMessage": {
					if (_this.onSysMessage != null)
						_this.onSysMessage(data);
					break;
				}
				
				case "userList": {
					if (_this.onUserList != null)
						_this.onUserList(data.Users);
					break;
				}
			}
		};
	}

	_this.disconnect = function() {
		closeSocket();
	};
	
	var send = function(obj) {
		try {
			socket.send(JSON.stringify(obj));
		} catch (err) { console.log(err); }
	};
	
	_this.login = function(user, pass, tokens, room) {
		send({Type: "auth", Method: "login", Username: user, Password: pass, Tokens: tokens, Room: room});
	};
	
	_this.register = function(user, pass) {
		send({Type: "auth", Method: "register", Username: user, Password: pass});
	};
	
	_this.requestHistory = function(afterDate) {
		send({Type: "chatHistoryRequest", AfterDate: afterDate});
	};
	
	_this.sendMessage = function(message) {
		send({Type: "sendMessage", Content: message});
	};
}