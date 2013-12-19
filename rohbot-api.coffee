throw new Error("Where's the microevent library?!") unless window.MicroEvent?
class window.RohBot
	constructor: (@url)->
		# Useful infos
		@name = null;

		manualSysMessage = (message) =>
			@trigger "sysmessage", 
				Date: Date.now() / 1000
				Content: message

		manualSysMessage "Connecting to RohBot..."
		@connect url

		# polite connection management
		connected = false
		@on "connected", =>
			manualSysMessage "Connected to RohBot!" unless connected
			connected = true
		@on "disconnected", =>
			manualSysMessage "Lost connection to RohBot. Reconnecting..." if connected
			connected = false
			@disconnect() # IDK it's in the js api
		window.setInterval =>
			if @isConnected()
				@_send "ping", {}
			else
				@connect()
		, 2500

	connect: ->
		@disconnect();
		@socket = new WebSocket(@url);
		@socket.addEventListener 'open', => @trigger "connected" if @isConnected()
		@socket.addEventListener 'close', (e) =>
			console.info "socket closed", e;
			@trigger "disconnected"
		@socket.addEventListener 'error', (e) =>
			console.error "websocket error", e;
			@trigger "disconnected"
		@socket.addEventListener 'message', (event) => @_onMessage JSON.parse event.data
		# @socket.onopen = () -> console.info "onopen"

	disconnect: ->
		@socket.close if @socket?
		@socket = null

	isConnected: -> @socket?.readyState == WebSocket.OPEN

	login: (Username, Password, Tokens, Room) -> @_send "auth", { Method: "login", Username, Password, Tokens, Room }

	register: (Username, Password) -> @_send "auth", { Method: "register", Username, Password }

	requestHistory: (AfterDate) -> @_send "chatHistoryRequest", { AfterDate }

	sendMessage: (Content) -> @_send "sendMessage", { Content }


	_send: (type, data) ->
		# The existing api lib doesn't give a shit if you're not connected. That's p bad tbh.
		if ! @isConnected()
			console.error "Tried to send a", type, "message with payload", data, "without being connected!"
			return
		data['Type'] = type
		try
			@socket.send JSON.stringify data
		catch e
			console.error "Unable to send", type, "message (with payload", data, ") because: ", e.message

	_onMessage: (data) ->
		switch data.Type
			when "authResponse"
				@name = data.name
				@trigger "login", data
			when "chatHistory"
				@trigger "chathistory", data
			when "message"
				@trigger "message", data.Line
			when "sysMessage"
				@trigger "sysmessage", data
			when "userList"
				@trigger "userlist", data.Users

	MicroEvent.mixin(@)
	legacy = (name) =>
		Object.defineProperty @prototype, 'on' + name, set: (func) -> @on name.toLowerCase(), func, false

	legacy "Connected"
	legacy "Disconnected"
	legacy "Login"
	legacy "ChatHistory"
	legacy "SysMessage"
	legacy "Message"
	legacy "UserList"