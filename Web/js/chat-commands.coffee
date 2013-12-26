window.processChatCommands = (text) ->
	return false unless ! text.indexOf('~') || ! text.indexOf('/')
	command = text.substr( 1 ).toLowerCase();

	if ! command.indexOf 'clear'
		window.$('chat').empty();
	else if ! command.indexOf 'logout'
		window.rohbot.login("guest", null, null);
	else if ! command.indexOf 'password'
		pass = text.substr( 10 );
		if ! pass.length
			window.localStorage.removeItem 'password'
			window.chat.statusMessage 'Password removed.'
		else if pass.length < 6
			window.chat.statusMessage 'Password too short!'
		else
			window.localStorage.setItem 'password', pass
			window.chat.statusMessage 'Password saved.'
	else if ! command.indexOf 'notify'
		console.log "ye notificus"
	else
		return false
	return true