# Because localStorage isn't available on file:// urls (wtf??)

store = window.localStorage
if store?
	try
		datum = Date.now() + "a"
		store['localStorage test'] = datum
		store = null unless datum == store['localStorage test']
		if store
			delete store['localStorage test']
	catch
		store = null
unless store?
	store = {}

window.rohStore =
	get: (item) -> store[item]
	set: (item, value) -> store[item] = value
	delete: (item) -> delete store[item]

