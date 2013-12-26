# Dr Empty Class
if ! ( window.Notification? or window.webkitNotifications? )
	class window.NotificationCenter
		doNotification: ->
		enableNotifications: ->
	return
isSane = window.Notification?
Notification = window.Notification or {}
webkitNotifications = window.webkitNotifications or {}
class window.NotificationCenter
	constructor: () ->
		@denied = false;
		@hasPermission = false;
		if window.localStorage.get( 'notifications-enabled' )
			@getPermission()
			# Just in case we're using an older ver of the speck, ask on the first click as well.
			clickus = () =>
				document.removeEventListener 'click', clickus
				@getPermission()
			document.addEventListener 'click', clickus

	getPermission: (cback = null) ->
		if @hasPermission or Notification.permission == 'granted' or webkitNotifications.checkPermission?() == 0
			@hasPermission = true
			cback?()
		else if @denied or Notification.permission == 'denied' or webkitNotifications.checkPermission?() == 2
			@denied = true
		else
			if isSane
				Notification.requestPermission (permission) =>
					if permission == 'granted'
						@hasPermission = true
						cback?()
					else
						@denied = true
			else
				webkitNotifications.requestPermission () =>
					if webkitNotifications.checkPermission() == 0
						@hasPermission = true
						cback?()
					else
						@denied = true

	doNotification: (title, body) ->
		return unless @hasPermission
		if isSane
			noti = new Notification title,
				icon: 'rohpod.png'
				body: body
			denoti = ->
				noti?.close()
				noti = null 
		else
			noti = webkitNotifications.createNotification 'rohpod.png', title, body
			denoti = ->
				noti?.cancel()
				noti = null 

		setTimeout denoti, 3000
		noti.addEventListener 'click', denoti
		noti.show?()

	enableNotifications: () ->
		@getPermission();
		window.localStorage.set( 'notifications-enabled', true )

