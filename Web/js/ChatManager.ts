
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
        history.scroll(e => {
            if (history[0].scrollTop <= 0 || history[0].scrollTop > 64)
                return;

            var currentChat = this.getCurrentChat();
            if (currentChat == null)
                return;

            currentChat.requestHistory();
        });
    }

    switchTo(shortName: string) {
        var target = this.getChat(shortName);
        if (target == null) {
            console.error("switchTo without existing chat:", shortName);
            return;
        }

        if (shortName != "home")
            RohStore.set("last-chat", shortName);

        $("#history > *").each((i, e) => {
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

            chat.addLine(packet.Line, false);
        });

        rohbot.userListReceived.add(packet => {
            var chat = this.getCurrentChat();
            if (chat == null) {
                console.warn("userList without chat");
                return;
            }

            chat.statusMessage("In this room:");

            var userMap = u => {
                if (u.Avatar == "0000000000000000000000000000000000000000")
                    u.Avatar = "fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb";

                if (u.Web)
                    u.Avatar = false;
                else
                    u.AvatarFolder = u.Avatar.substring(0, 2);

                if (u.Playing === "")
                    u.Playing = false;

                if (u.Status === "")
                    u.Status = "&nbsp;";

                if (u.Playing)
                    u.Status = "In-Game: " + u.Playing;

                if (u.Playing)
                    u.Color = "ingame";
                else if (u.Web)
                    u.Color = "web";
                else
                    u.Color = "";

                return u;
            };

            var html = templates.users.render({
                Users: packet.Users.map(u => userMap(u))
            });

            chat.addHtml(html);
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
