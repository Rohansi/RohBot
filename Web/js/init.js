
var rohbot = new RohBot("wss://fpp.literallybrian.com/ws/");
var chatMgr = new ChatManager(rohbot);
var ui = new UserInterface(rohbot, chatMgr);
