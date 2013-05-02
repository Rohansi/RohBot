RohBot = function(server) {
	var _this = this;
	
	var socket = null;
	var firstConnect = true;
	var hasConnected = false;
	
	_this.onconnected = null;
	_this.ondisconnected = null;
	_this.onlogin = null;
	_this.onsysmessage = null;
	_this.onmessage = null;
	_this.onuserlist = null;
	_this.onuserdata = null;
	
	var getCurrentTime = function() {
		return new Date().getTime() / 1000;
	};
	
	setInterval(function () {
		if (socket !== null)
			send({ Type: "ping" });
	}, 500);
	
	_this.connect = function() {
		if (firstConnect) {
			if (_this.onsysmessage != null)
				_this.onsysmessage({Date: getCurrentTime(), Content: "Connecting to RohBot..."});
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
		
		socket.onclose = function (event) {
			socket = null;
			
			if (hasConnected) {
				if (_this.onsysmessage != null)
					_this.onsysmessage({Date: getCurrentTime(), Content: "Lost connection to RohBot. Retrying..."});
				hasConnected = false;
			}
			
			if (_this.ondisconnected != null)
				_this.ondisconnected();
			
			setTimeout(function () {
				_this.connect();
			}, 1000);
		};
		
		socket.onmessage = function (event) {
			var data = JSON.parse(event.data);
			
			switch (data.Type)
			{
				case "clientPermissions": {
					if (_this.onlogin != null)
						_this.onlogin(data);
					break;
				}
				
				case "chatHistory": {
					for (var i = 0; i < data.Lines.length; i++) {
						if (_this.onmessage != null)
							_this.onmessage(data.Lines[i]);
					}
					break;
				}
				
				case "message": {
					if (_this.onmessage != null)
						_this.onmessage(data.Line);
					break;
				}
				
				case "sysMessage": {
					if (_this.onsysmessage != null)
						_this.onsysmessage(data);
					break;
				}
				
				case "userList": {
					if (_this.onuserlist != null)
						_this.onuserlist(data.Users);
					break;
				}
				
				case "userData": {
					if (data.Action != "loaded") break;
					if (_this.onuserdata != null)
						_this.onuserdata(data.Data);
				}
			}
		};
	}

	var send = function(obj) {
		try {
			socket.send(JSON.stringify(obj));
		} catch (err) { }
	};
	
	_this.login = function(user, pass) {
		send({Type: "login", Username: user, Password: pass});
	};
	
	_this.sendMessage = function(message) {
		send({Type: "sendMessage", Content: message});
	};
	
	_this.loadUserData = function() {
		send({Type: "userData", Action: "load"});
	};
	
	_this.storeUserData = function(data) {
		send({Type: "userData", Action: "store", Data: data});
	};
}