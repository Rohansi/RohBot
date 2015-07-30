var server = "wss://" + window.location.host + "/ws/";
if (window.location.protocol !== "https:")
    server = "ws://107.191.98.153:12000/";

var rohbot = new RohBot(server);
var chatMgr = new ChatManager(rohbot);
var ui = new UserInterface(rohbot, chatMgr);

function send(room, message) {
    ui.sendPressed.trigger(room, message);
}

function join(room) {
    if (chatMgr.getChat(room) === null)
        send("home", "/join " + room);
    else
        chatMgr.switchTo(room);
}
