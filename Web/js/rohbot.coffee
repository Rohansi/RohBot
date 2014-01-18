window.initializeRohBot = ->
	roomName = null
	requestedHistory = false
	oldestMessage = 0xFFFFFFFF

	rohBotPls = ->
		server = 'wss://fpp.literallybrian.com/ws/'
		if window.location.search == '?noproxy'
			server = 'ws://fpp.literallybrian.com:12000/'
		new RohBot server

	window.rohbot = rohbot = rohBotPls()
	window.chatMgr = chatMgr = new ChatManager rohbot
	window.loginMgr = loginMgr = new LoginManager
	window.notifications = notifications = new NotificationCenter

	store = window.rohStore
	rohbot.on 'connected', ->
		requestedHistory = false
		oldestMessage = 0xFFFFFFFF

		room = window.location.hash.substr 1
		loginMgr.autoLogin room

	rohbot.on 'login', (info) ->
		if info.Success
			$('#header').hide()
			$('#messageBox').attr('disabled', false).val('')
		else
			$('#header').show()
			$('#messageBox').attr('disabled', true).val('Guests cannot speak!')

		chatMgr.scrollToBottom()

	rohbot.on 'chathistory', (data) ->
		chatMgr.addChatHistory data.Lines, data.Requested

		requestedHistory = false if data.Requested

		roomName = data.Name
		document.title = roomName
		$('#title').text roomName
		oldestMessage = data.OldestLine

	p = document.createElement 'p'
	htmlDecode = (str) ->
		p.innerHTML = str
		p.textContent

	rohbot.on 'message', (line) ->
		chatMgr.addLine line
		if line.Type == 'chat' and line.Sender != rohbot.name and line.SenderId != "0"
			if notifications.checkMessage line.Content
				notifications.doNotification roomName,
					htmlDecode line.Sender + ': ' + line.Content

	rohbot.on 'sysmessage', (line) ->
		line.Type = 'state'
		chatMgr.addLine line

	userListFilter = (user) -> user.Name != 'Guest'

	userListMap = (user) ->
		if user.Avatar == "0000000000000000000000000000000000000000"
			user.Avatar = "fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb"
		if user.Web
			user.Avatar = false
		else
			user.AvatarFolder = user.Avatar.substring(0, 2)
		# Explicit falsy to false
		user.Playing = false unless user.Playing
		user.Color = if user.Playing then "ingame" else if user.Web then "web" else ""
		return user

	rohbot.on 'userlist', (users) ->
		chatMgr.statusMessage 'In this room: '
		chatMgr.addHtml templates.users.render
			Users: users.filter(userListFilter).map(userListMap)

	rohbot.connect()

	$("#chat").on 'scroll', ->
		if this.scrollTop == 0 and not requestedHistory
			rohbot.requestHistory oldestMessage
			requestHistory = true

	$(window).on 'resize', -> chatMgr.scrollToBottom()
