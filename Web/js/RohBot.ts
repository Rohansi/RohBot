
declare var templates;
declare var urlize;

class RohBot {

    private address: string;
    private socket: WebSocket;
    private isConnecting: boolean;
    private username: string;

    onConnected: () => void;
    onDisconnected: () => void;
    onLogin: (packet) => void;
    onChat: (packet) => void;
    onChatHistory: (packet) => void;
    onSysMessage: (packet) => void;
    onMessage: (packet) => void;
    onUserList: (packet) => void;

    constructor(address: string) {
        this.address = address;
        this.username = null;

        window.setInterval(() => {
            if (this.isConnected()) {
                this.send({ Type: "ping" });
            } else {
                this.connect();
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

            if (!connected)
                this.manualSysMessage("Connected to RohBot!");

            connected = true;

            if (this.onConnected != null)
                this.onConnected();
        };

        var wsClosed = e => {
            this.isConnecting = false;

            if (connected)
                this.manualSysMessage("Lost connection to RohBot. Reconnecting...");

            connected = false;

            if (this.onDisconnected != null)
                this.onDisconnected();

            this.disconnect();
        };

        this.socket.onclose = wsClosed;

        this.socket.onerror = e => {
            wsClosed(e);
            console.error("websocket error", e);
        };

        this.socket.onmessage = e => {
            var packet = JSON.parse(e.data);

            switch (packet.Type) {
                case "authResponse": {
                    this.username = packet.Name;
                    if (this.username != null && this.username.length == 0)
                        this.username = null;

                    if (this.onLogin != null)
                        this.onLogin(packet);

                    break;
                }

                case "chat": {
                    if (this.onChat != null)
                        this.onChat(packet);
                    break;
                }

                case "chatHistory": {
                    if (this.onChatHistory != null)
                        this.onChatHistory(packet);
                    break;
                }

                case "message": {
                    if (this.onMessage != null)
                        this.onMessage(packet);
                    break;
                }

                case "sysMessage": {
                    if (this.onSysMessage != null)
                        this.onSysMessage(packet);
                    break;
                }

                case "userList": {
                    if (this.onUserList != null)
                        this.onUserList(packet);
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

    login(username: string, password: string) {
        this.send({
            Type: "auth",
            Method: "login",
            Username: username,
            Password: password
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
        if (this.onSysMessage != null) {
            this.onSysMessage({
                Type: "sysMessage",
                Date: new Date().getTime() / 1000,
                Content: message
            });
        }
    }

    private send(packet) {
        try {
            this.socket.send(JSON.stringify(packet));
        } catch (e) { }
    }

}
