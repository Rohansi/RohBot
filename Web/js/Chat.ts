
class Chat {

    chatMgr: ChatManager;
    name: string;
    shortName: string;
    history: JQuery;
    tab: JQuery;
    users: JQuery;

    private requestedHistory: boolean;
    private oldestLine: number;
    private unreadMessages: number;
    private userList: any[];
    private userListDirty: boolean;
    private lastUserListChange: number;
    private lastUserListRefresh: number;

    constructor(chatMgr: ChatManager, name: string, shortName: string) {
        this.chatMgr = chatMgr;
        this.name = name;
        this.shortName = shortName;

        this.history = $(templates.history.render({ ShortName: shortName }));
        this.history.appendTo("#history").hide();

        this.tab = $(templates.tab.render({ Name: name, ShortName: shortName }));

        this.tab.click(e => {
            this.chatMgr.switchTo(this.shortName);
            return false;
        });

        var tabClose = this.tab.find(".tab-close");

        if (shortName == "home") {
            tabClose.hide();
        } else {
            tabClose.click(e => {
                this.chatMgr.rohbot.sendMessage(this.shortName, "/leave " + this.shortName);
                return false;
            });

            this.chatMgr.rohbot.sendMessage(shortName, "/users");
        }

        this.tab.appendTo("#tabs");

        this.users = $(templates.userlist.render({ ShortName: shortName }));
        this.users.appendTo("#users").hide();

        this.userList = null;
        this.userListDirty = false;
        this.lastUserListChange = Date.now();

        this.requestedHistory = false;
        this.oldestLine = 0;
        this.unreadMessages = 0;
    }

    destroy() {
        if (this.shortName == "home")
            return;
        
        this.history.remove();
        this.tab.remove();
        this.users.remove();
    }

    update() {
        var updateAfter = 2.5 * 1000;
        var refreshAfter = 45 * 1000; // forced
        var now = Date.now();

        if ((this.userListDirty && (now - this.lastUserListChange >= updateAfter)) || (now - this.lastUserListRefresh >= refreshAfter)) {
            this.chatMgr.rohbot.sendMessage(this.shortName, "/users");
            this.lastUserListChange = now;
            this.lastUserListRefresh = now;
        }
    }

    incrementUnreadCounter() {
        this.unreadMessages++;
        this.updateUnreadCounter();
    }

    resetUnreadCounter() {
        this.unreadMessages = 0;
        this.updateUnreadCounter();
    }

    requestHistory() {
        if (this.requestedHistory)
            return;

        this.requestedHistory = true;
        this.chatMgr.rohbot.requestHistory(this.shortName, this.oldestLine);
    }

    completeName(word: string) {
        word = word.toLowerCase();

        for (var i = 0; i < this.userList.length; i++) {
            var user = this.userList[i];
            var name = $("<textarea/>").html(user.Name).text();

            if (name.toLowerCase().indexOf(word) == 0)
                return name;
        }

        return null;
    }

    addHistory(data: any) {
        var history: any[] = data.Lines;

        if (!data.Requested) {
            this.history.empty();

            for (var i = 0; i < history.length; i++) {
                this.addLine(history[i], false);
            }

            if (this.isActive())
                this.chatMgr.scrollToBottom();
        } else {
            var prevHeight = this.history[0].clientHeight;

            for (var i = history.length - 1; i >= 0; i--) {
                this.addLine(history[i], true);
            }

            var currHeight = this.history[0].clientHeight;

            if (this.isActive())
                this.chatMgr.scrollRelative(currHeight - prevHeight - 16);

            this.requestedHistory = false;
        }

        this.oldestLine = data.OldestLine;
    }

    statusMessage(message: string) {
        this.addLine({
            Type: "state",
            State: "Client",
            Date: Date.now(),
            Content: message
        });
    }

    renderLine(line: any) {
        var timeFmt = RohStore.get("time format");
        if (timeFmt == null) timeFmt = "12hr";

        var date = new Date(line.Date);

        var data: any = {
            DateTime: date.toISOString(),
            Message: line.Content
        };

        if (timeFmt != "off")
            data.Time = Chat.formatTime(date, timeFmt);

        switch (line.Type) {
            case "chat": {
                var senderClasses = "";

                if (line.UserType == "RohBot")
                    senderClasses = "rohBot " + line.SenderStyle;
                else if (line.InGame)
                    senderClasses = "inGame";

                if (line.UserType == "Steam" && line.SenderId != "0")
                    data.SteamId = line.SenderId;

                data.Sender = line.Sender;
                data.SenderClasses = senderClasses;
                data.Message = this.linkify(line.Content);
                break;
            }

            case "state":
                if (line.State == "Action" || line.State == "Client")
                    break;

                var style = t => t == "Steam" ? "steam" : "rohBot";
                var stateData: any = {
                    For: line.For,
                    ForStyle: style(line.ForType),
                };

                if (line.ForType == "Steam" && line.ForId != "0")
                    stateData.ForSteamId = line.ForId;

                if (line.By != "") {
                    stateData.By = line.By;
                    stateData.ByStyle = style(line.ByType);

                    if (line.ByType == "Steam" && line.ById != "0")
                        stateData.BySteamId = line.ById;
                }

                switch (line.State) {
                    case "Enter":
                        stateData.Content1 = " entered chat.";
                        break;

                    case "Left":
                        stateData.Content1 = " left chat.";
                        break;

                    case "Disconnected":
                        stateData.Content1 = " disconnected.";
                        break;

                    case "Kicked":
                        stateData.Content1 = " was kicked by ";
                        stateData.Content2 = ".";
                        break;

                    case "Banned":
                        stateData.Content1 = " was banned by ";
                        stateData.Content2 = ".";
                        break;

                    case "Unbanned":
                        stateData.Content1 = " was unbanned by ";
                        stateData.Content2 = ".";
                        break;

                    default:
                        console.warn("unhandled state type", line.State);
                        break;
                }

                data.Message = templates.statemessage.render(stateData);
                break;

            default:
                console.error("unsupported line type", line);
                break;
        }

        return templates.message.render(data);
    }

    addLine(line: any, prepend: boolean = false) {
        if (!prepend && this.userList != null)
            this.applyStateLine(line);

        this.addHtml(this.renderLine(line), prepend);
    }

    addHtml(html: string, prepend: boolean = false) {
        var historyElem = $("#history")[0];
        var atBottom = historyElem.clientHeight >= (historyElem.scrollHeight - historyElem.scrollTop - 32);

        if (prepend)
            this.history.prepend(html);
        else
            this.history.append(html);

        if (this.isActive && !prepend && atBottom)
            this.chatMgr.scrollToBottom();
    }

    setUserList(userList: any[]) {
        var now = Date.now();

        this.userList = userList;
        this.userListDirty = false;
        this.lastUserListChange = now;
        this.lastUserListRefresh = now;

        this.renderUserList();
    }

    private applyStateLine(line: any) {
        if (line.Type != "state")
            return;

        switch (line.State) {
            case "Enter":
                this.userList.push({
                    Name: line.For,
                    UserId: line.ForId,
                    Rank: "Member",
                    Avatar: "0000000000000000000000000000000000000000",
                    Status: line.ForType == "RohBot" ? "" : "Online",
                    Playing: "",
                    Web: line.ForType == "RohBot"
                });
                break;

            case "Banned":
                if (line.ForType == "RohBot")
                    break;
            case "Left":
            case "Disconnected":
                this.userList = this.userList.filter(e => {
                    return e.Web != (line.ForType == "RohBot") || e.Name != line.For;
                });
                break;

            default:
                return;
        }

        this.userListDirty = true;
        this.lastUserListChange = Date.now();

        this.renderUserList();
    }

    private renderUserList() {
        this.userList.sort((a, b) => {
            var aLower = a.Name.toLowerCase();
            var bLower = b.Name.toLowerCase();

            if (aLower < bLower)
                return -1;
            if (aLower > bLower)
                return 1;
            return 0;
        });

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
                u.Status = u.Playing;

            if (u.Playing)
                u.Color = "ingame";
            else if (u.Web)
                u.Color = "web";
            else
                u.Color = "";

            return u;
        };

        var html = templates.users.render({
            Users: this.userList.map(u => userMap(u))
        });

        this.users.html(html);
    }

    private updateUnreadCounter() {
        if (this.isActive())
            this.unreadMessages = 0;

        var title = this.name;
        if (this.unreadMessages > 0) {
            var countStr = this.unreadMessages > 99 ? "99+" : this.unreadMessages.toString();
            title = "(" + countStr + ") " + title;
        }

        this.tab.find(".tab-name").text(title);
    }

    private isActive() {
        var currentChat = this.chatMgr.getCurrentChat();
        return currentChat != null && currentChat.shortName == this.shortName;
    }

    private linkify(text: string) {
        // Put spaces infront of <s to stop urlize seizing them as urls
        text = text.replace(/ː(\w+?)ː/g, ' ː<img src="/economy/emoticon/$1" alt=":$1:" class="emote">');
        text = urlize(text, { target: "_blank" });
        text = text.replace('\n', ' <br>'); // whitespace infront of a <br> isn't noticable
        text = text.replace(/\ ː/g, ''); // Get rid of the sentinel chars. (triangle colons are guaranteed to never appear in normal text)
        return text;
    }

    static formatTime(date: Date, timeFmt: string) {
        if (timeFmt == "off")
            return "";

        var hours: any = date.getHours();
        var minutes: any = date.getMinutes();
        var military = timeFmt == "24hr";
        var suffix = "";

        if (military) {
            if (hours < 10)
                hours = '0' + hours;
        } else {
            suffix = " AM";
            if (hours >= 12) {
                suffix = " PM";
                hours -= 12;
            }

            if (hours == 0)
                hours = 12;
        }

        if (minutes < 10)
            minutes = "0" + minutes;

        suffix += "&nbsp;&#8209;&nbsp;";
        return hours + ":" + minutes + suffix;
    }
}
