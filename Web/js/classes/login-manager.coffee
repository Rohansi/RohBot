class window.LoginManager
	constructor: () ->
		@username = $ '#username'
		@password = $ '#password'

		rohbot.on 'login', (info) =>
			rohStore.set 'name', info.Name
			rohStore.set 'tokens', info.Tokens

			@password.val '' if info.Success

		$('#loginButton').on 'click', =>
			@login @username.val(), @password.val()

		$('#registerButton').on 'click', =>
			rohbot.register @username.val(), @password.val()


	autoLogin: (room) ->
		name = rohStore.get 'name'
		password = rohStore.get 'password'
		tokens = rohStore.get 'tokens'
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

	login: (Username, Password) ->
		rohbot.login {
			Username,
			Password
		}

	logout: -> rohbot.login Username: 'guest'

	savePassword: (password) -> rohStore.set 'password', password
	forgetPassword: -> rohStore.delete 'password'
