
function htmlUnescape(value: string): string {
    return $("<textarea/>").html(value).text();
}

class ChatManager {

    private chats: { [shortName: string]: Chat };
    rohbot: RohBot;

    private loggingIn: boolean;

    lineFilter: Event3<HistoryLine, boolean, { filtered: boolean }> = new TypedEvent();
    userFilter: Event2<UserListUser, { filtered: boolean }> = new TypedEvent();

    constructor(rohbot: RohBot) {
        this.chats = {};
        this.rohbot = rohbot;
        this.loggingIn = true;

        this.setupRohBot(this.rohbot);

        this.joinChat("Home", "home");
        this.switchTo("home");

        var history = $("#history");
        var historyElem = history[0];
        history.scroll(() => {
            if (historyElem.scrollTop > 0 || historyElem.scrollHeight <= historyElem.clientHeight)
                return;

            var currentChat = this.getCurrentChat();
            if (currentChat == null)
                return;

            currentChat.requestHistory();
        });

        window.setInterval(() => {
            if (this.rohbot.getUsername() == null)
                return;

            var chat = this.getCurrentChat();
            if (chat != null)
                chat.update();
        }, 250);
    }

    switchTo(shortName: string) {
        var target = this.getChat(shortName);
        if (target == null) {
            console.error("switchTo without existing chat:", shortName);
            return;
        }

        if (shortName !== "home")
            RohStore.set("last-chat", shortName);

        target.resetUnreadCounter();

        $("#history > *").each((i, e) => {
            var elem = $(e);

            if (elem.data("name") === shortName)
                elem.show();
            else
                elem.hide();
        });

        $("#users > *").each((i, e) => {
            var elem = $(e);

            if (elem.data("name") === shortName)
                elem.show();
            else
                elem.hide();
        });

        $("#tabs > *").removeClass("selected");
        target.tab.addClass("selected");

        this.scrollToBottom();
    }

    getChat(shortName: string) {
        if (!this.chats.hasOwnProperty(shortName))
            return null;

        return this.chats[shortName];
    }

    getCurrentChat() {
        var visible = $("#history > *").filter(":visible");

        if (visible.length === 0)
            return null;

        if (visible.length >= 2)
            console.error("multiple chat histories are visible!");

        return this.chats[visible.data("name")];
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
        rohbot.loggedIn.add(() => {
            this.loggingIn = true;

            for (var k in this.chats) {
                if (!this.chats.hasOwnProperty(k) || k === "home")
                    continue;

                var chat = this.chats[k];
                chat.destroy();
                delete this.chats[k];
            }

            this.switchTo("home");
        });

        rohbot.chatReceived.add(packet => {
            if (packet.Method === "join") {
                this.joinChat(packet.Name, packet.ShortName);
            } else if (packet.Method === "leave") {
                this.leaveChat(packet.ShortName);
            }
        });

        rohbot.chatHistoryReceived.add(packet => {
            var chat = this.getChat(packet.ShortName);
            if (chat == null) {
                console.error("chatHistory without existing chat:", packet.ShortName);
                return;
            }

            packet.Lines = packet.Lines.filter(line => {
                var result = { filtered: false };
                this.lineFilter.trigger(line, true, result);
                return !result.filtered;
            });

            chat.addHistory(packet);
        });

        rohbot.sysMessageReceived.add(packet => {
            var chat = this.getCurrentChat();
            if (chat == null) {
                console.error("sysMessage without chat");
                return;
            }

            var loginMsg = "Logged in as";
            if (this.loggingIn && packet.Content.substring(0, loginMsg.length) === loginMsg) {
                this.loggingIn = false;

                var lastChat = RohStore.get("last-chat");
                if (lastChat != null)
                    this.switchTo(lastChat);
            }

            var sysMsgLine = {
                Type: "state",
                Date: packet.Date,
                Chat: chat.shortName,
                Content: packet.Content,
                State: "Client"
            }

            chat.addLine(sysMsgLine, false);
        });

        rohbot.messageReceived.add(packet => {
            var line = packet.Line;
            var chat = this.getChat(line.Chat);

            if (chat == null) {
                console.warn("message without existing chat:", line.Chat);
                return true;
            }
            
            var result = { filtered: false };
            this.lineFilter.trigger(line, false, result);
            if (result.filtered)
                return false;

            if (line.Type === "chat" && (<ChatLine>line).SenderId !== "0") {
                chat.incrementUnreadCounter();
            }

            chat.addLine(line, false);
            return true;
        });

        rohbot.userListReceived.add(packet => {
            var chat = this.getChat(packet.ShortName);
            if (chat == null) {
                console.warn("userList without existing chat");
                return;
            }

            packet.Users = packet.Users.filter(user => {
                var result = { filtered: false };
                this.userFilter.trigger(user, result);
                return !result.filtered;
            });

            chat.setUserList(packet.Users);
        });
    }

    private joinChat(name: string, shortName: string) {
        if (this.chats.hasOwnProperty(shortName)) {
            console.warn("joinChat on existing chat:", shortName);
            return;
        }

        this.chats[shortName] = new Chat(this, name, shortName);

        if (!this.loggingIn)
            this.switchTo(shortName);
    }

    private leaveChat(shortName: string) {
        if (!this.chats.hasOwnProperty(shortName))
            return;

        if (shortName === "home")
            return;

        this.chats[shortName].destroy();
        delete this.chats[shortName];

        if (this.getCurrentChat() == null) {
            var last = $("#tabs > *").last().data("name");
            if (last != null)
                this.switchTo(last);
        }
    }
}
