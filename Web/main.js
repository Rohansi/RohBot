var WebServer = require("./WebServer.js").WebServer;

var server = new WebServer(80, "www");
server.start();