RohBot
===========
RohBot is a web interface for Steam group chats. Try it live here: https://fpp.rohbot.net/

Commands
--------------
Commands must be prefixed with the tilde character (~) to work. The website also allows a forward slash (/) to be used instead.

### join ###
Allows you to join a room. `/join fp`

### me ###
Does exactly what you think it does. `/me bites Brian.` will send `YourName bites Brian.` to the current room.

### clear ###
Clears the room's history.

### logout ###
Logs out of the website.

### notify ###
Enable or disable notifications on supported browsers. `/notify brian` will make notifications appear whenever somebody says Brian. `/notify` will disable noficiations. More complex conditions can be used by supplying a [regex](http://en.wikipedia.org/wiki/Regular_expression) pattern.

### timeformat ###
Allows you to switch time formats. Available options are `/timeformat 12hr`, `/timeformat 24hr` or `/timeformat off`.

### password ###
Saves your password in the browser for automatic login. This is only useful if your IP changes a lot and hate logging in. `/password thisismypasswordshh` will save and `/password` will remove.

### banned ###
Display a list of accounts that are banned from the current room.

### ban ###
Bans an account from the room. Can only be used by moderators. `/ban stan` will ban Stan from the room.

### unban ###
Unbans an account from the room. Can only be used by moderators. `/unban stan` will unban Stan from the room.

### mod ###
Promotes an account to moderator. Can only be used by administrators.

### demod ###
Demotes an account from moderator. Can only be used by administrators.

### modded ###
Displays a list of modded accounts for the room.

Permissions
-------------
Permissions are unique to rooms.

| Name            | Description                                                          |
|-----------------|----------------------------------------------------------------------|
| Super Admin     | Global permission to everything. The person running the bot.         |
| Administrator   | Owner of the room.                                                   |
| Moderator       | Administrator's choice (mod command) or silver/gold star on Steam.   |
