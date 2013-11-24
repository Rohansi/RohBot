SteamMobile
===========
Steam web interface for group chats.

Commands
--------------
Commands must be prefixed with the tilde character (~) to work. The website also allows a forward slash (/) to be used instead.

### room ###
Allows you to switch between rooms on the website.
- `/room` display which room you are in
- `/room fp` switch to room fp
- `/room default` switch to your default room
- `/room default fp` set your default room to fp and switch to it
- `/room list` show a list of available rooms

### users ###
Display a list of users in the room.

### me ###
Does exactly what you think it does. `/me bites Brian.` will send `YourName bites Brian.` to the current room.

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

### sessions ###
Displays a list of accounts that are logged in.

Permissions
-------------
Permissions are unique to rooms.

| Name            | Description                                                        |
|-----------------|--------------------------------------------------------------------|
| Super Admin     | Global permission to everything. The person running the bot.       |
| Administrator   | Administrator for the room. Gold star or super admin's choice.     |
| Moderator       | Moderator for the room. Admin's choice.                            |

