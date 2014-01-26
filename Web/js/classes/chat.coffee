class window.ChatManager
	constructor: (@rohbot) ->
		@chat  = $ '#chat'
		@form  = $ '#chat-form'
		@input = $ '#message-box'

		@input.on 'keydown', @processEnter
		@form.on  'submit',   @processSend

	processSend: (e) =>
		e.preventDefault()
		message = @input.val()
		return unless message.length

		unless @processCommand message
			@rohbot.sendMessage message

		@input.val ''

	processEnter: (event) =>
		# Converted from http://stackoverflow.com/a/3533099/1056845
		return unless event.keyCode == 13
		unless event.ctrlKey
			event.preventDefault()
			@form.submit()
			return
		dom = @input[0]
		if typeof dom.selectionStart == "number" and typeof dom.selectionEnd == "number"
			start = dom.selectionStart
			val = dom.value
			dom.value = val.slice( 0, start ) + "\n" + val.slice( dom.selectionEnd )
			dom.selectionStart = dom.selectionEnd = start + 1
		else if document.selection && document.selection.createRange
			dom.focus()
			range = document.selection.createRange()
			range.text = "\r\n"
			range.collapse(false)
			range.select()

	clearChat: -> @chat.empty()

	processCommand: (text) ->
		text = text.trim()
		return false unless ! text.indexOf('~') || ! text.indexOf('/')
		command = text.substr( 1 ).toLowerCase()

		if ! command.indexOf 'clear'
			@clearChat()
		else if ! command.indexOf 'logout'
			loginMgr.logout()
		else if ! command.indexOf 'password'
			pass = text.substr 10
			if ! pass.length
				loginMgr.forgetPassword()
				@statusMessage 'Password removed.'
			else if pass.length < 6
				@statusMessage 'Password too short!'
			else
				loginMgr.savePassword pass
				@statusMessage 'Password saved.'
		else if ! command.indexOf 'notify'
			if command.length <= 7
				notifications.disableNotifications()
				@statusMessage 'Notifications disabled!'
			else
				res = false
				res = notifications.setNotificationRegex text.substr 8
				if res
					@statusMessage 'Invalid Regex: ' + res
				else
					@statusMessage 'Notification regex saved!'
		else if ! command.indexOf 'timeformat'
			fmt = text.substr 12
			if fmt == '24hr'
				window.rohStore.set 'clock format', '24hr'
				@statusMessage 'Clock set to 24hr'
			else if fmt == '12hr'
				window.rohStore.set 'clock format', '12hr'
				@statusMessage 'Clock set to 12hr'
			else
				@statusMessage 'Unknown clock format ' + fmt + '! Try 12hr or 24hr.'
				return
			@chat.find('time').each( (a,b) =>
				me = $ b
				me.text @formatTime new Date me.attr 'datetime'
			)
		else
			return false
		return true

	addChatHistory: (history, requested) ->
		if requested
			history.reverse()
			first = @chat.children(':first')
		else
			@clearChat()

		history.forEach (line) => @addLine line, requested

		if requested
			# Attempt to scroll to the first new line
			if first[0]
				# Compensate for the header
				header = $ '#header'
				offset = header.is(':visible') ? header.height() + 10 : 0
				@chat.scrollTop first.offset().top - offset - 20
		else
			@scrollToBottom()

	statusMessage: (text) ->
		@addLine
			Type: 'state'
			Date: Date.now() / 1000
			Content: text

	formatTime: ( date ) ->
		hours = date.getHours()
		military = '24hr' == window.rohStore.get 'clock format'
		if military
			if hours < 10
				hours = '0' + hours
		else
			suffix = 'AM'
			if hours >= 12
				suffix = 'PM'
				hours -= 12
			if hours == 0
				hours = 12
			if hours < 10
				hours = ' ' + hours
		minutes = date.getMinutes()
		if minutes < 10
			minutes = '0' + minutes
		if military
			return "#{hours}:#{minutes}"
		else
			return "#{hours}:#{minutes} #{suffix}"

	linkify: (text) ->
		# Put spaces infront of <s to stop urlize seizing them as urls
		text = text.replace '\n', ' <br>' # whitespace infront of a <br> isn't noticable
		text = text.replace /ː(.+?)ː/g, ' ː<img src="/economy/emoticon/$1" alt="$1" class="emote">' # It is on an <img> though
		text = urlize text, target: '_blank'
		text = text.replace /\ ː/g, '' # Get rid of the sentinel chars. (triangle colons are guaranteed to never appear in normal text)


	addLine: (data, prepend) ->
		date = new Date( data.Date * 1000 )
		line =
			Time: @formatTime date
			DateTime: date.toISOString()
			Message: data.Content
		switch data.Type
			when 'chat'
				senderClasses = ''
				if data.UserType == 'RohBot'
					senderClasses = 'rohBot'
					if data.SenderStyle
						senderClasses += ' ' + data.SenderStyle
				else if data.InGame
					senderClasses = 'inGame'
				line.Sender = data.Sender
				line.SenderClasses = senderClasses
				line.Message = @linkify line.Message
			when 'state' then
				# Seems fine
			when 'whisper'
				data.Type = 'chat'
				if data.Sender == @rohbot.name
					data.Sender = 'To ' + data.Receiver
				else
					data.Sender = 'From ' + data.Sender
				return @addLine data
			else
				console.error "Unknown line type!", data
				return

		@addHtml templates.message.render( line ), prepend

	addHtml: (html, prepend) ->
		chat = @chat[0]

		atBottom = @chat.outerHeight() >= ( chat.scrollHeight - chat.scrollTop - 32 )

		if prepend
			@chat.prepend html
		else
			@chat.append html

		if ! prepend && atBottom
			@scrollToBottom()

	scrollToBottom: ->
		chat = @chat[0]
		chat.scrollTop = chat.scrollHeight
