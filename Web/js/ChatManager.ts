
class ChatManager {

    private chats: { [shortName: string]: Chat; };
    rohbot: RohBot;

    constructor(rohbot: RohBot) {
        this.chats = {};
        this.rohbot = rohbot;

        this.setupRohBot(this.rohbot);
    }

    switchTo(shortName: string) {
        var target = this.getChat(shortName);
        if (target == null) {
            console.warn("switchTo without existing chat:", shortName);
            return;
        }

        $("#history > *").hide();
        target.history.show();
        this.scrollToBottom();
    }

    getChat(shortName: string) {
        if (!(shortName in this.chats))
            return null;

        return this.chats[shortName];
    }

    getCurrentChat() {
        var visible = $("#history > *").filter(":visible");

        if (visible.length == 0)
            return null;

        if (visible.length >= 2)
            console.warn("multiple chat histories are visible!");

        return this.chats[visible.attr("data-name")];
    }

    scrollToBottom() {
        var historyElem = $("#history")[0];
        historyElem.scrollTop = historyElem.scrollHeight;
    }

    private setupRohBot(rohbot: RohBot) {
        rohbot.onChat = packet => {
            if (packet.Method == "join") {
                this.joinChat(packet.Name, packet.ShortName);
            } else if (packet.Method == "leave") {
                this.leaveChat(packet.ShortName);
            }
        };

        rohbot.onChatHistory = packet => {
            var chat = this.getChat(packet.ShortName);
            if (chat == null) {
                console.warn("onChatHistory without existing chat:", packet.ShortName);
                return;
            }

            chat.addHistory(packet.Lines, packet.Requested);
        };

        rohbot.onSysMessage = packet => {
            var chat = this.getCurrentChat();
            if (chat == null) {
                console.warn("onSysMessage without chat");
                return;
            }

            // TODO
        };

        rohbot.onMessage = packet => {
            var chat = this.getChat(packet.Line.Chat);
            if (chat == null) {
                console.warn("onMessage without existing chat:", packet.Line.Chat);
                return;
            }

            chat.addLine(packet.Line, false);
        };

        rohbot.onUserList = packet => {
            var chat = this.getCurrentChat();
            if (chat == null) {
                console.warn("onUserList without chat");
                return;
            }

            // TODO
        };
    }

    private joinChat(name: string, shortName: string) {
        if (shortName in this.chats) {
            console.warn("joinChat on existing chat:", shortName);
            return;
        }

        this.chats[shortName] = new Chat(this, name, shortName);
        this.switchTo(shortName);
    }

    private leaveChat(shortName: string) {
        if (!(shortName in this.chats))
            return;

        this.chats[shortName].destroy();
        delete this.chats[shortName];
    }
}
