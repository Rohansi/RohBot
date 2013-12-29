# RohBot

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
window.notifications = notifications = new NotificationCenter

store = window.rohStore
rohbot.on 'connected', ->
	requestedHistory = false
	oldestMessage = 0xFFFFFFFF

	room = window.location.hash.substr 1
	name = store.get 'name'
	password = store.get 'password'
	tokens = store.get 'tokens'
	if name and (password or tokens)
		rohbot.login
			Username: name
			Password: password
			Tokens: tokens
			Room: room
	else
		rohbot.login
			Username: 'guest'
			Room: room

rohbot.on 'login', (info) ->
	storage.set 'name', info.Name
	storage.set 'tokens', info.Tokens

	if info.Success
		$('#header').hide()
		$('#messageBox').attr('disabled', false).val('')
		$('#password').val('')
	else
		$('#header').show()
		$('#messageBox').attr('disabled', true).val('Guests cannot speak!')

	chatMgr.scrollToBottom()

rohbot.on 'chatHistory', (data) ->
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
	if line.Type == 'chat' and line.Sender != rohbot.name
		if notifications.checkMessage line.Content
			notifications.doNotification roomName,
				htmlDecode line.Sender + ': ' + line.Content

rohbot.on 'sysMessage', (line) ->
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
	user.Color = user.Playing ? "ingame" : ( user.Web ? "web" : "" )
	return user

rohbot.on 'userList', (users) ->
	chatMgr.statusMessage 'In this room: '
	chatMgr.addHtml templates.users.render
		Users: users.filter(userListFilter).map(userListMap)

rohbot.connect()
