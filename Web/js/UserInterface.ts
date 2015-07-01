
class UserInterface {

    rohbot: RohBot;
    chatMgr: ChatManager;

    private notificationRegex: RegExp;
    private unreadMessages: number;

    loginPressed: Event2<string, string> = new TypedEvent();
    registerPressed: Event2<string, string> = new TypedEvent();
    sendPressed: Event2<string, string> = new TypedEvent();
    
    constructor(rohbot: RohBot, chatMgr: ChatManager) {
        this.rohbot = rohbot;
        this.chatMgr = chatMgr;

        this.setupHandlers();
        this.setChatEnabled(false);

        if (this.setNotificationRegex(RohStore.get("notifications-regex"))) {
            this.notificationRegex = null;
            RohStore.remove("notifications-regex");
        }

        this.rohbot.connected.add(() => {
            var username = RohStore.get("name");
            var password = RohStore.get("password");
            var tokens = RohStore.get("tokens");

            if (username != null && (password != null || tokens != null)) {
                this.rohbot.login(username, password, tokens);
            } else {
                this.rohbot.loginGuest();
            }
        });

        this.rohbot.loggedIn.add(packet => {
            this.setChatEnabled(packet.Success && this.rohbot.getUsername() != null);

            if (packet.Name === null) {
                RohStore.remove("name");
                RohStore.remove("tokens");
                RohStore.remove("password");
            } else if (packet.Success) {
                RohStore.set("name", packet.Name);
                RohStore.set("tokens", packet.Tokens);

                $("#password").val("");
            }
        });

        this.rohbot.messageReceived.add(packet => {
            var line = packet.Line;
            var chatLine = <ChatLine>line;
            var stateLine = <StateLine>line;

            if (line.Type === "chat" && (<ChatLine>line).SenderId !== "0") {
                this.unreadMessages++;
                this.updateUnreadCounter();
            }

            if (this.notificationRegex != null && Visibility.hidden()) {
                var isNotifiableChatLine = line.Type === "chat" && chatLine.SenderId !== "0" && !(chatLine.UserType === "RohBot" && chatLine.Sender === this.rohbot.getUsername());
                var isNotifiableStateLine = line.Type === "state" && stateLine.State === "Action" && !(stateLine.ForType === "RohBot" && stateLine.For === this.rohbot.getUsername());

                if ((isNotifiableChatLine || isNotifiableStateLine) && this.notificationRegex.test(line.Content)) {
                    var chat = this.chatMgr.getChat(line.Chat);
                    if (chat == null)
                        return;

                    var notificationText;

                    switch (line.Type) {
                        case "chat":
                            var sender = $("<textarea/>").html(chatLine.Sender).text();
                            var content = $("<textarea/>").html(chatLine.Content).text();
                            notificationText = sender + ": " + content;
                            break;
                        case "state":
                            notificationText = $("<textarea/>").html(stateLine.Content).text();
                            break;
                        default:
                            notificationText = "error";
                            break;
                    }

                    Notifications.create(chat.name, notificationText, () => {
                        chatMgr.switchTo(line.Chat);
                        window.focus();
                    });
                }
            }
        });

        this.loginPressed.add((username, password) => {
            this.rohbot.login(username, password);
        });

        this.registerPressed.add((username, password) => {
            this.rohbot.register(username, password);
        });

        this.sendPressed.add((roomName, message) => {
            if (message.length === 0)
                return;

            if (this.processCommand(message))
                return;

            this.rohbot.sendMessage(roomName, message);
        });

        this.unreadMessages = 0;

        Visibility.changed.add(() => {
            if (Visibility.visible()) {
                this.unreadMessages = 0;
                this.updateUnreadCounter();

                $("#message-box").focus();
            }
        });
    }

    setChatEnabled(enabled: boolean) {
        if (enabled) {
            $("#header").hide();
            $("#message-box").removeAttr("disabled").val("");
        } else {
            $("#header").show();
            $("#message-box").attr("disabled", "true").val("guests can't speak");
        }
    }

    private updateUnreadCounter() {
        if (!Visibility.hidden())
            this.unreadMessages = 0;

        var title = "RohBot";

        if (this.unreadMessages > 0) {
            var countStr = this.unreadMessages > 99 ? "99+" : this.unreadMessages.toString();
            title = "(" + countStr + ") " + title;
        }

        window.document.title = title;
    }

    private processCommand(message: string): boolean {
        message = message.trim();

        if (message.indexOf("/") !== 0 && message.indexOf("~") !== 0)
            return false;

        var chat = this.chatMgr.getCurrentChat();
        if (chat == null)
            return false;

        var command = message.substr(1).toLowerCase();

        if (command.indexOf("clear") === 0) {
            chat.history.empty();
        } else if (command.indexOf("logout") === 0) {
            this.rohbot.loginGuest();
        } else if (command.indexOf("password") === 0) {
            var pass = message.substr(10);
            if (pass.length === 0) {
                RohStore.remove("password");
                chat.statusMessage("Password removed.");
            } else if (pass.length < 6) {
                chat.statusMessage("Password too short.");
            } else {
                RohStore.set("password", pass);
                chat.statusMessage("Password saved.");
            }
        } else if (command.indexOf("notify") === 0) {
            if (!Notifications.areSupported()) {
                chat.statusMessage("Your browser doesn't support notifications.");
            } else if (command.length <= 7) {
                Notifications.disable();
                chat.statusMessage("Notifications disabled.");
            } else {
                var err = this.setNotificationRegex(message.substr(8));
                if (err) {
                    chat.statusMessage("Regex error: " + err);
                } else {
                    Notifications.enable();
                    chat.statusMessage("Notification regex saved.");
                }
            }
        } else if (command.indexOf("timeformat") === 0) {
            var oldFmt = RohStore.get("time format");
            if (oldFmt == null) oldFmt = "12hr";

            var newFmt = command.substr(11);
            if (newFmt === "24hr") {
                chat.statusMessage("Time format set to 24hr.");
            } else if (newFmt === "12hr") {
                chat.statusMessage("Time format set to 12hr.");
            } else if (newFmt === "off") {
                chat.statusMessage("Time disabled.");
            } else {
                chat.statusMessage("Unknown times format '" + newFmt + "'. Try 12hr, 24hr or off.");
                return true;
            }

            RohStore.set("time format", newFmt);

            if (oldFmt === "off" && newFmt !== "off") {
                $("#history time").removeClass("hidden");
            } else if (oldFmt !== "off" && newFmt === "off") {
                $("#history time").addClass("hidden");
            }

            if (newFmt !== "off") {
                $("#history time").each((i, e) => {
                    var j = $(e);
                    j.html(Chat.formatTime(new Date(j.attr("datetime")), newFmt));
                });
            }
        } else if (command.indexOf("users") === 0) {
            chat.history.find("> ol.inline-users").remove();
            chat.addHtml("<ol class=\"user-list inline-users\">" + chat.users.html() + "</ol>");
        } else {
            return false;
        }

        return true;
    }

    private setupHandlers() {
        var username = $("#username");
        var password = $("#password");
        var loginBtn = $("#loginButton");
        var registerBtn = $("#registerButton");

        loginBtn.click(e => {
            e.preventDefault();
            this.loginPressed.trigger(username.val(), password.val());
        });

        registerBtn.click(e => {
            e.preventDefault();
            this.registerPressed.trigger(username.val(), password.val());
        });

        var send = $("#send");
        var messageBox = $("#message-box");

        send.click(e => {
            e.preventDefault();

            var currentChat = this.chatMgr.getCurrentChat();
            if (currentChat == null)
                return;

            this.sendPressed.trigger(currentChat.shortName, messageBox.val());
            messageBox.val("");
            messageBox.focus();
        });

        messageBox.keydown(e => {
            var dom: any = messageBox[0];
            var val = dom.value;

            if (e.keyCode === 13) { // enter
                if (e.ctrlKey) {
                    if (typeof dom.selectionStart == "number" && typeof dom.selectionEnd == "number") {
                        var start = dom.selectionStart;
                        dom.value = val.slice(0, start) + "\n" + val.slice(dom.selectionEnd);
                        dom.selectionStart = dom.selectionEnd = start + 1;
                    }
                } else {
                    send.click();
                }

                return false;
            } else if (e.keyCode === 9) { // tab
                if (typeof dom.selectionStart == "number" && typeof dom.selectionEnd == "number") {
                    if (dom.selectionStart === dom.selectionEnd) {
                        this.doNameCompletion(dom);
                    }
                }

                return false;
            } else {
                this.completionNames = null;
            }

            return true;
        });

        $(document).keypress(e => {
            // todo: this might have caused a weird bug with firefox and ctrl+a on textboxes

            if (typeof e.which == "undefined"
                || $(e.target).is("input, textarea")
                || this.rohbot.getUsername() == null
                || e.which <= 0
                || e.which === 8
                || e.ctrlKey
                || e.metaKey
                || e.altKey) {
                return;
            }

            e.preventDefault();

            messageBox.focus();
            messageBox.val(messageBox.val() + String.fromCharCode(e.which));
        });

        $(window).resize(() => {
            this.chatMgr.scrollToBottom();
        });
    }

    private completionNames: string[] = null;
    private completionIndex: number = 0;
    private completionStart: number = 0;
    private completionEnd: number = 0;

    private doNameCompletion(dom: any) {
        var val = dom.value;

        var chat = this.chatMgr.getCurrentChat();
        if (chat == null)
            return;

        if (this.completionNames == null) {
            var wordStart = val.slice(0, dom.selectionStart).lastIndexOf(" ") + 1;

            var completionWord = val.slice(wordStart, dom.selectionStart);
            if (completionWord.length === 0)
                return;

            this.completionNames = chat.getCompletionNames(completionWord);
            if (this.completionNames.length === 0) {
                this.completionNames = null;
                return;
            }

            this.completionNames.push(completionWord);

            this.completionIndex = 0;
            this.completionStart = wordStart;
            this.completionEnd = dom.selectionEnd;
        } else {
            this.completionIndex++;
            this.completionIndex %= this.completionNames.length;
        }

        var completionStr = this.completionNames[this.completionIndex];

        if (this.completionIndex !== this.completionNames.length - 1) {
            if (this.completionStart === 0)
                completionStr += ":";

            completionStr += " ";
        }

        dom.value = val.slice(0, this.completionStart) + completionStr + val.slice(this.completionEnd);
        dom.selectionStart = dom.selectionEnd = this.completionStart + completionStr.length;
        this.completionEnd = dom.selectionEnd;
    }

    private setNotificationRegex(source: string) {
        if (source == null) {
            this.notificationRegex = null;
            return false;
        }

        try {
            var regex = new RegExp(source, "gim");
            this.notificationRegex = regex;
            RohStore.set("notifications-regex", regex.source);
        } catch (e) {
            return e.message;
        }

        return false;
    }

}
