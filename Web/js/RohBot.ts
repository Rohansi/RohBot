
declare var templates;

class RohBot {

    private address: string;
    private socket: WebSocket;
    private isConnecting: boolean;
    private hasConnected: boolean;
    private username: string;

    private timeout: number;
    private lastMessage: number;

    connected: Event0 = new TypedEvent();
    disconnected: Event0 = new TypedEvent();
    loggedIn: Event1<AuthResponsePacket> = new TypedEvent();
    chatReceived: Event1<ChatPacket> = new TypedEvent();
    chatHistoryReceived: Event1<ChatHistoryPacket> = new TypedEvent();
    sysMessageReceived: Event1<SysMessagePacket> = new TypedEvent();
    messageReceived: Event1<MessagePacket> = new TypedEvent();
    userListReceived: Event1<UserListPacket> = new TypedEvent();

    constructor(address: string) {
        this.timeout = 45 * 1000;

        this.address = address;
        this.username = null;

        window.setInterval(() => {
            if (!this.isConnecting && !this.isConnected()) {
                this.connect();
            }

            if (this.isConnected() && Date.now() - this.lastMessage >= this.timeout) {
                console.log("timed out");
                this.disconnect();
            }
        }, 5000);

        this.manualSysMessage("Connecting to RohBot...");
        this.connect();
    }

    private connect() {
        this.disconnect();
        this.hasConnected = false;
        this.isConnecting = true;

        this.socket = new WebSocket(this.address);

        this.socket.onopen = () => {
            this.isConnecting = false;
            this.lastMessage = Date.now();

            if (!this.hasConnected)
                this.manualSysMessage("Connected to RohBot!");

            this.hasConnected = true;

            this.connected.trigger();
        };

        this.socket.onclose = () => this.disconnect();

        this.socket.onerror = e => {
            console.error("websocket error", e);
            this.disconnect();
        };

        this.socket.onmessage = e => {
            var packet = JSON.parse(e.data);

            this.lastMessage = Date.now();

            switch (packet.Type) {
                case "authResponse": {
                    this.username = packet.Name;
                    if (this.username != null && this.username.length === 0)
                        this.username = null;

                    this.loggedIn.trigger(packet);
                    break;
                }

                case "chat": {
                    this.chatReceived.trigger(packet);
                    break;
                }

                case "chatHistory": {
                    this.chatHistoryReceived.trigger(packet);
                    break;
                }

                case "message": {
                    this.messageReceived.trigger(packet);
                    break;
                }

                case "sysMessage": {
                    this.sysMessageReceived.trigger(packet);
                    break;
                }

                case "userList": {
                    this.userListReceived.trigger(packet);
                    break;
                }
            }
        };
    }

    private disconnect() {
        if (this.hasConnected)
            this.manualSysMessage("Lost connection to RohBot. Reconnecting...");

        this.isConnecting = false;
        this.hasConnected = false;
        this.username = null;

        if (this.socket != null) {
            this.socket.close();
            this.socket.onopen = null;
            this.socket.onclose = null;
            this.socket.onerror = null;
            this.socket.onmessage = null;
            this.socket = null;
        }

        this.disconnected.trigger();
    }

    isConnected(): boolean {
        if (this.socket == null)
            return false;
        return this.socket.readyState === 1;
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

    requestUserList(roomName: string) {
        this.send({
            Type: "userListRequest",
            Target: roomName
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
        this.sysMessageReceived.trigger({
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
