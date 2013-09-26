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
	
	setInterval(function () {
		if (socket !== null)
			send({ Type: "ping" });
	}, 2500);
	
	_this.connect = function() {
		if (firstConnect) {
			if (_this.onSysMessage != null)
				_this.onSysMessage({Date: getCurrentTime(), Content: "Connecting to RohBot..."});
			firstConnect = false;
		}
		
		if (socket !== null) {
			if (socket.readyState == 1)
				socket.close();
			socket.onopen = null;
			socket.onclose = null;
			socket.onmessage = null;
			socket = null;
		}

		socket = new WebSocket(server);
		
		socket.onopen = function (event) {
			if (_this.onsysmessage != null)
				_this.onsysmessage({Date: getCurrentTime(), Content: "Connected to RohBot!"});
			if (_this.onconnected != null)
				_this.onconnected();
			hasConnected = true;
		};
		
		socket.onclose = socket.onerror = function (event) {
			socket = null;
			
			if (hasConnected) {
				if (_this.onSysMessage != null)
					_this.onSysMessage({Date: getCurrentTime(), Content: "Lost connection to RohBot."});
				hasConnected = false;
			}
			
			if (_this.onDisconnected != null)
				_this.onDisconnected();
			
			setTimeout(function () {
				_this.onSysMessage({Date: getCurrentTime(), Content: "Connecting to RohBot..."});
				_this.connect();
			}, 5000);
		};
		
		socket.onmessage = function (event) {
			var data = JSON.parse(event.data);
			
			switch (data.Type)
			{
				case "authResponse": {
					_this.name = data.Name;
					if (_this.onLogin != null)
						_this.onLogin();
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
		socket.close();
	};
	
	var send = function(obj) {
		try {
			socket.send(JSON.stringify(obj));
		} catch (err) { }
	};
	
	_this.login = function(user, pass, tokens) {
		send({Type: "auth", Method: "login", Username: user, Password: pass, Tokens: tokens});
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