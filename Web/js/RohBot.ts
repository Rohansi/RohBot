
declare var templates;
declare var urlize;
declare var Visibility;

class RohBot {

    private address: string;
    private socket: WebSocket;
    private isConnecting: boolean;
    private username: string;

    private timeout: number;
    private lastMessage: number;

    connected = new Signal();
    disconnected = new Signal();
    loggedIn = new Signal();
    chatReceived = new Signal();
    chatHistoryReceived = new Signal();
    sysMessageReceived = new Signal();
    messageReceived = new Signal();
    userListReceived = new Signal();

    constructor(address: string) {
        this.timeout = 10 * 1000;

        this.address = address;
        this.username = null;

        window.setInterval(() => {
            if (!this.isConnecting && !this.isConnected()) {
                this.connect();
            }

            if (this.isConnected() && Date.now() - this.lastMessage >= this.timeout) {
                this.disconnect();
            }
        }, 2500);

        this.manualSysMessage("Connecting to RohBot...");
        this.connect();
    }

    private connect() {
        this.disconnect();
        this.isConnecting = true;

        this.socket = new WebSocket(this.address);
        var connected = false;

        this.socket.onopen = e => {
            this.isConnecting = false;
            this.lastMessage = Date.now();

            if (!connected)
                this.manualSysMessage("Connected to RohBot!");

            connected = true;

            this.connected.dispatch();
        };

        var wsClosed = e => {
            this.isConnecting = false;

            if (connected)
                this.manualSysMessage("Lost connection to RohBot. Reconnecting...");

            connected = false;

            this.disconnect();
            this.disconnected.dispatch();
        };

        this.socket.onclose = wsClosed;

        this.socket.onerror = e => {
            wsClosed(e);
            console.error("websocket error", e);
        };

        this.socket.onmessage = e => {
            var packet = JSON.parse(e.data);

            this.lastMessage = Date.now();

            switch (packet.Type) {
                case "authResponse": {
                    this.username = packet.Name;
                    if (this.username != null && this.username.length == 0)
                        this.username = null;

                    this.loggedIn.dispatch(packet);
                    break;
                }

                case "chat": {
                    this.chatReceived.dispatch(packet);
                    break;
                }

                case "chatHistory": {
                    this.chatHistoryReceived.dispatch(packet);
                    break;
                }

                case "message": {
                    this.messageReceived.dispatch(packet);
                    break;
                }

                case "sysMessage": {
                    this.sysMessageReceived.dispatch(packet);
                    break;
                }

                case "userList": {
                    this.userListReceived.dispatch(packet);
                    break;
                }
            }
        };
    }

    private disconnect() {
        this.username = null;

        if (this.socket == null)
            return;

        this.socket.close();
        this.socket.onopen = null;
        this.socket.onclose = null;
        this.socket.onerror = null;
        this.socket.onmessage = null;
        this.socket = null;
    }

    isConnected(): boolean {
        if (this.socket == null)
            return false;
        return this.socket.readyState == WebSocket.OPEN;
    }

    getUsername(): string {
        return this.username;
    }

    login(username: string, password: string, tokens: string = null) {
        this.send({
            Type: "auth",
            Method: "login",
            Username: username,
            Password: password,
            Tokens: tokens
        });
    }

    loginGuest() {
        this.send({
            Type: "auth",
            Method: "guest"
        });
    }

    register(username: string, password: string) {
        this.send({
            Type: "auth",
            Method: "register",
            Username: username,
            Password: password
        });
    }

    requestHistory(roomName: string, afterDate: number) {
        this.send({
            Type: "chatHistoryRequest",
            Target: roomName,
            AfterDate: afterDate
        });
    }

    sendMessage(roomName: string, message: string) {
        this.send({
            Type: "sendMessage",
            Target: roomName,
            Content: message
        });
    }

    private manualSysMessage(message: string) {
        this.sysMessageReceived.dispatch({
            Type: "sysMessage",
            Date: Date.now(),
            Content: message
        });
    }

    private send(packet) {
        try {
            this.socket.send(JSON.stringify(packet));
        } catch (e) {
            console.error("send", e);
        }
    }

}
