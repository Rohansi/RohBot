
class ChatManager {

    private chats: { [shortName: string]: Chat; };
    rohbot: RohBot;

    private loggingIn: boolean;

    constructor(rohbot: RohBot) {
        this.chats = {};
        this.rohbot = rohbot;
        this.loggingIn = true;

        this.setupRohBot(this.rohbot);

        this.joinChat("Home", "home");
        this.switchTo("home");

        var history = $("#history");
        var historyElem = history[0];
        history.scroll(e => {
            if (historyElem.scrollTop > 0 || historyElem.scrollHeight <= historyElem.clientHeight)
                return;

            var currentChat = this.getCurrentChat();
            if (currentChat == null)
                return;

            currentChat.requestHistory();
        });

        window.setInterval(() => {
            var chat = this.getCurrentChat();
            if (chat != null)
                chat.update();

            /*for (var key in this.chats) {
                var value = this.chats[key];
                if (!value.hasOwnProperty(key))
                    continue;

                value.update();
            }*/
        }, 250);
    }

    switchTo(shortName: string) {
        var target = this.getChat(shortName);
        if (target == null) {
            console.error("switchTo without existing chat:", shortName);
            return;
        }

        if (shortName != "home")
            RohStore.set("last-chat", shortName);

        target.resetUnreadCounter();

        $("#history > *").each((i, e) => {
            var elem = $(e);

            if (elem.attr("data-name") == shortName)
                elem.show();
            else
                elem.hide();
        });

        $("#users > *").each((i, e) => {
            var elem = $(e);

            if (elem.attr("data-name") == shortName)
                elem.show();
            else
                elem.hide();
        });

        $("#tabs > *").removeClass("selected");
        target.tab.addClass("selected");

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
            console.error("multiple chat histories are visible!");

        return this.chats[visible.attr("data-name")];
    }

    scrollToBottom() {
        var historyElem = $("#history")[0];
        historyElem.scrollTop = historyElem.scrollHeight;
    }

    scrollTo(position: number) {
        var historyElem = $("#history")[0];
        historyElem.scrollTop = position;
    }

    scrollRelative(amount: number) {
        var historyElem = $("#history")[0];
        historyElem.scrollTop += amount;
    }

    private setupRohBot(rohbot: RohBot) {
        rohbot.loggedIn.add(packet => {
            this.loggingIn = true;

            for (var k in this.chats) {
                if (k == "home")
                    continue;

                var chat = this.chats[k];
                chat.destroy();
                delete this.chats[k];
            }

            this.switchTo("home");
        });

        rohbot.chatReceived.add(packet => {
            if (packet.Method == "join") {
                this.joinChat(packet.Name, packet.ShortName);
            } else if (packet.Method == "leave") {
                this.leaveChat(packet.ShortName);
            }
        });

        rohbot.chatHistoryReceived.add(packet => {
            var chat = this.getChat(packet.ShortName);
            if (chat == null) {
                console.error("chatHistory without existing chat:", packet.ShortName);
                return;
            }

            chat.addHistory(packet);
        });

        rohbot.sysMessageReceived.add(packet => {
            var chat = this.getCurrentChat();
            if (chat == null) {
                console.error("sysMessage without chat");
                return;
            }

            var loginMsg = "Logged in as";
            if (this.loggingIn && packet.Content.substring(0, loginMsg.length) == loginMsg) {
                this.loggingIn = false;

                var lastChat = RohStore.get("last-chat");
                if (lastChat != null)
                    this.switchTo(lastChat);
            }

            packet.Type = "state";
            packet.State = "Client";
            chat.addLine(packet, false);
        });

        rohbot.messageReceived.add(packet => {
            var chat = this.getChat(packet.Line.Chat);
            if (chat == null) {
                console.warn("message without existing chat:", packet.Line.Chat);
                return;
            }

            if (packet.Line.Type == "chat")
                chat.incrementUnreadCounter();

            chat.addLine(packet.Line, false);
        });

        rohbot.userListReceived.add(packet => {
            var chat = this.getChat(packet.ShortName);
            if (chat == null) {
                console.warn("userList without existing chat");
                return;
            }

            chat.setUserList(packet.Users);
        });
    }

    private joinChat(name: string, shortName: string) {
        if (shortName in this.chats) {
            console.warn("joinChat on existing chat:", shortName);
            return;
        }

        this.chats[shortName] = new Chat(this, name, shortName);

        if (!this.loggingIn)
            this.switchTo(shortName);
    }

    private leaveChat(shortName: string) {
        if (!(shortName in this.chats))
            return;

        if (shortName == "home")
            return;

        this.chats[shortName].destroy();
        delete this.chats[shortName];

        if (this.getCurrentChat() == null) {
            var last = $("#tabs > *").last().attr("data-name");
            if (last != null)
                this.switchTo(last);
        }
    }
}
