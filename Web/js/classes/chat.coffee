class window.ChatManager
	constructor: (@rohbot) ->
		@chat  = $ '#chat'
		@input = $ '#messageBox'
		@send  = $ '#send'

		@input.on 'keydown', @processEnter
		@send.on  'click',   @processSend

	processSend: =>
		message = @input.val()
		return unless message.length

		unless @processCommand message
			@rohbot.sendMessage message

		@input.val ''

	processEnter: (event) =>
		# Converted from http://stackoverflow.com/a/3533099/1056845
		return unless event.keyCode == 13
		unless event.ctrlKey
			@input.blur().focus()
			@send.click()
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
			@rohbot.login("guest", null, null)
		else if ! command.indexOf 'password'
			pass = text.substr( 10 )
			if ! pass.length
				window.localStorage.removeItem 'password'
				@statusMessage 'Password removed.'
			else if pass.length < 6
				@statusMessage 'Password too short!'
			else
				window.localStorage.setItem 'password', pass
				@statusMessage 'Password saved.'
		else if ! command.indexOf 'notify'
			if command.length <= 7
				res = false # Hi Coffeelint
				# notifications.disableNotifications
			else
				res = false
				# res = notifications.setNotificationRegex message.substr 8
				if res
					@statusMessage 'Invalid Regex: ' + res
				else
					@statusMessage 'Notification regex saved!'
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
