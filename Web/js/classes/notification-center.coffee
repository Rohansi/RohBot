# Dr Empty Class
if ! ( window.Notification? or window.webkitNotifications? )
	class window.NotificationCenter
		doNotification: ->
		enableNotifications:  ->
		disableNotifications: ->
		setNotificationRegex: ->
		checkMessage: ->
	return
isSane = window.Notification?
Notification = window.Notification or {}
webkitNotifications = window.webkitNotifications or {}
class window.NotificationCenter
	constructor: () ->
		@denied = false
		@hasPermission = false
		@regex = null
		@enabled = false
		if window.rohStore.getItem 'notifications-enabled'
			@enabled = true
			@getPermission()
			# Just in case we're using an older ver of the speck, ask on the first click as well.
			clickus = () =>
				document.removeEventListener 'click', clickus
				@getPermission()
			document.addEventListener 'click', clickus
			# Load the stored notification regex
			if regex = window.rohStore.getItem 'notifications-regex'
				try @regex = new RegExp regex, 'gim'


	getPermission: () ->
		if @hasPermission or Notification.permission == 'granted' or webkitNotifications.checkPermission?() == 0
			@hasPermission = true
		else if @denied or Notification.permission == 'denied' or webkitNotifications.checkPermission?() == 2
			@denied = true
		else
			if isSane
				Notification.requestPermission (permission) =>
					if permission == 'granted'
						@hasPermission = true
					else
						@denied = true
			else
				webkitNotifications.requestPermission () =>
					if webkitNotifications.checkPermission() == 0
						@hasPermission = true
					else
						@denied = true

	doNotification: (title, body) ->
		return unless @hasPermission
		if isSane
			noti = new Notification title,
				icon: 'rohbot.png'
				body: body
			denoti = ->
				noti?.close()
				noti = null
		else
			noti = webkitNotifications.createNotification 'rohbot.png', title, body
			denoti = ->
				noti?.cancel()
				noti = null

		setTimeout denoti, 3000
		noti.addEventListener 'click', denoti
		noti.show?()

	enableNotifications: () ->
		@getPermission()
		window.rohStore.setItem 'notifications-enabled', true
		@enabled = true

	disableNotifications: () ->
		window.rohStore.deleteItem 'notifications-enabled'
		@enabled = false

	setNotificationRegex: ( str ) ->
		try
			regex = new RegExp str, 'gim'
		catch e
			return e.message
		@enableNotifications()
		@regex = regex
		window.rohStore.setItem 'notifications-regex', regex.source
		return false

	checkMessage: (message) -> @enabled and @regex? and @regex.test message
