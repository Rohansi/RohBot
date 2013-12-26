class window.ChatManager
	constructor: (@rohbot)->
		#beep boop

	processCommand: (text) ->
		return false unless ! text.indexOf('~') || ! text.indexOf('/')
		command = text.substr( 1 ).toLowerCase();

		if ! command.indexOf 'clear'
			window.$('chat').empty();
		else if ! command.indexOf 'logout'
			@rohbot.login("guest", null, null);
		else if ! command.indexOf 'password'
			pass = text.substr( 10 );
			if ! pass.length
				window.localStorage.removeItem 'password'
				@statusMessage 'Password removed.'
			else if pass.length < 6
				@statusMessage 'Password too short!'
			else
				window.localStorage.setItem 'password', pass
				@statusMessage 'Password saved.'
		else if ! command.indexOf 'notify'
			console.log "ye notificus"
		else
			return false
		return true

	statusMessage: (text) ->
		@addLine
			Type: 'state'
			Date: Date.now() / 1000
			Content: text

	addLine: (data) ->
		console.error("Not implemented!", data)
		# TODO
