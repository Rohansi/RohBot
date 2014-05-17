
var rohbot = new RohBot("wss://fpp.literallybrian.com/ws/");
var chatMgr = new ChatManager(rohbot);
var ui = new UserInterface(rohbot, chatMgr);

function send(room, message) {
    ui.sendPressed.trigger(room, message);
}

function join(room) {
    if (chatMgr.getChat(room) == null)
        send("home", "/join " + room);
    else
        chatMgr.switchTo(room);
}
