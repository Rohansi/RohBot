
class UserInterface {

    rohbot: RohBot;
    chatMgr: ChatManager;

    private notificationRegex: RegExp;

    loginPressed = new Signal();
    registerPressed = new Signal();
    sendPressed = new Signal();

    constructor(rohbot: RohBot, chatMgr: ChatManager) {
        this.rohbot = rohbot;
        this.chatMgr = chatMgr;

        this.setupHandlers();
        this.setChatEnabled(false);

        if (this.setNotificationRegex(RohStore.get("notifications-regex"))) {
            this.notificationRegex = null;
            RohStore.delete("notifications-regex");
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
                RohStore.delete("name");
                RohStore.delete("tokens");
                RohStore.delete("password");
            } else if (packet.Success) {
                RohStore.set("name", packet.Name);
                RohStore.set("tokens", packet.Tokens);

                $("#password").val("");
            }
        });

        this.rohbot.messageReceived.add(packet => {
            var line = packet.Line;

            if (line.Type != "chat" || line.SenderId == "0" || line.Sender == this.rohbot.getUsername())
                return;

            if (this.notificationRegex != null && this.notificationRegex.test(line.Content)) {
                var chat = this.chatMgr.getChat(line.Chat);
                if (chat == null)
                    return;

                var sender = $("<textarea/>").html(line.Sender).text();
                var content = $("<textarea/>").html(line.Content).text();
                Notifications.create(chat.name, sender + ": " + content);
            }
        });

        this.loginPressed.add((username, password) => {
            this.rohbot.login(username, password);
        });

        this.registerPressed.add((username, password) => {
            this.rohbot.register(username, password);
        });

        this.sendPressed.add((roomName, message) => {
            if (message.length == 0)
                return;

            if (this.processCommand(message))
                return;

            this.rohbot.sendMessage(roomName, message);
        });
    }

    setChatEnabled(enabled: boolean) {
        if (enabled) {
            $("#header").hide();
            $("#message-box").removeAttr("disabled").val("");
        } else {
            $("#header").show();
            $("#message-box").attr("disabled", "true").val("Guests can not speak!");
        }
    }

    private processCommand(message: string): boolean {
        message = message.trim();

        if (message.indexOf("/") != 0 && message.indexOf("~") != 0)
            return false;

        var chat = this.chatMgr.getCurrentChat();
        if (chat == null)
            return false;

        var command = message.substr(1).toLowerCase();

        if (command.indexOf("clear") == 0) {
            chat.history.empty();
        } else if (command.indexOf("logout") == 0) {
            this.rohbot.loginGuest();
        } else if (command.indexOf("password") == 0) {
            var pass = message.substr(10);
            if (pass.length == 0) {
                RohStore.delete("password");
                chat.statusMessage("Password removed.");
            } else if (pass.length < 6) {
                chat.statusMessage("Password too short!");
            } else {
                RohStore.set("password", pass);
                chat.statusMessage("Password saved.");
            }
        } else if (command.indexOf("notify") == 0) {
            if (!Notifications.areSupported()) {
                chat.statusMessage("Your browser doesn't support notifications.");
            } else if (command.length <= 7) {
                Notifications.disable();
                chat.statusMessage("Notifications disabled!");
            } else {
                var err = this.setNotificationRegex(message.substr(8));
                if (err) {
                    chat.statusMessage("Regex error: " + err);
                } else {
                    Notifications.enable();
                    chat.statusMessage("Notification regex saved!");
                }
            }
        } else if (command.indexOf("timeformat") == 0) {
            var oldFmt = RohStore.get("time format");
            if (oldFmt == null) oldFmt = "12hr";

            var newFmt = command.substr(11);
            if (newFmt == "24hr") {
                chat.statusMessage("Time format set to 24hr.");
            } else if (newFmt == "12hr") {
                chat.statusMessage("Time format set to 12hr.");
            } else if (newFmt == "off") {
                chat.statusMessage("Time disabled.");
            } else {
                chat.statusMessage("Unknown times format '" + newFmt + "'. Try 12hr, 24hr or off.");
                return true;
            }

            RohStore.set("time format", newFmt);

            if (oldFmt == "off" && newFmt != "off") {
                $("#history time").removeClass("hidden");
            } else if (oldFmt != "off" && newFmt == "off") {
                $("#history time").addClass("hidden");
            }

            if (newFmt != "off") {
                $("#history time").each((i, e) => {
                    var j = $(e);
                    j.html(Chat.formatTime(new Date(j.attr("datetime")), newFmt));
                });
            }
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
            this.loginPressed.dispatch(username.val(), password.val());
        });

        registerBtn.click(e => {
            e.preventDefault();
            this.registerPressed.dispatch(username.val(), password.val());
        });

        var send = $("#send");
        var messageBox = $("#message-box");

        send.click(e => {
            e.preventDefault();
            var currentChat = this.chatMgr.getCurrentChat();
            if (currentChat == null)
                return;

            this.sendPressed.dispatch(currentChat.shortName, messageBox.val());
            messageBox.val("");
        });

        messageBox.keydown(e => {
            var dom: any = messageBox[0];

            if (e.keyCode == 13) {
                if (e.ctrlKey) {
                    var val = dom.value;
                    if (typeof dom.selectionStart == "number" && typeof dom.selectionEnd == "number") {
                        var start = dom.selectionStart;
                        dom.value = val.slice(0, start) + "\n" + val.slice(dom.selectionEnd);
                        dom.selectionStart = dom.selectionEnd = start + 1;
                    } else if (document.selection && document.selection.createRange) {
                        dom.focus();
                        var range = document.selection.createRange();
                        range.text = "\r\n";
                        range.collapse(false);
                        range.select();
                    }
                } else {
                    send.click();
                }

                return false;
            }
        });

        $(window).resize(() => {
            this.chatMgr.scrollToBottom();
        });
    }

    private setNotificationRegex(source: string) {
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
