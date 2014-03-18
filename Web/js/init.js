
var rohbot = new RohBot("ws://127.0.0.1:12000/");
var chatMgr = new ChatManager(rohbot);
var ui = new UserInterface(rohbot, chatMgr);
