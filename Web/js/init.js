
var rohbot = new RohBot("wss://fpp.literallybrian.com/ws/");
var chatMgr = new ChatManager(rohbot);
var ui = new UserInterface(rohbot, chatMgr);

function send(room, message) {
    ui.sendPressed.dispatch(room, message);
}

function join(room) {
    send("home", "/join " + room);
}
