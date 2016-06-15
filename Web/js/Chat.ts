
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
    private userList: UserListUser[];
    private userListDirty: boolean;
    private lastUserListChange: number;
    private lastUserListRefresh: number;

    constructor(chatMgr: ChatManager, name: string, shortName: string) {
        this.chatMgr = chatMgr;
        this.name = name;
        this.shortName = shortName;

        this.history = $(templates.history.render({ ShortName: this.shortName }));
        this.history.appendTo("#history").hide();

        this.tab = $(templates.tab.render({ Name: name, ShortName: this.shortName }));
        this.tab.appendTo("#tabs");

        this.tab.click(() => {
            this.chatMgr.switchTo(this.shortName);
            return false;
        });

        var tabClose = this.tab.find(".tab-close");

        if (this.shortName === "home") {
            tabClose.hide();
        } else {
            tabClose.click(() => {
                this.chatMgr.rohbot.sendMessage(this.shortName, "/leave " + this.shortName);
                return false;
            });
        }

        this.users = $(templates.userlist.render({ ShortName: this.shortName }));
        this.users.appendTo("#users").hide();

        this.userList = null;
        this.userListDirty = true;
        this.lastUserListChange = 0;
        this.lastUserListRefresh = 0;

        this.requestedHistory = false;
        this.oldestLine = 0;
        this.unreadMessages = 0;
    }

    destroy() {
        if (this.shortName === "home")
            return;

        this.history.remove();
        this.tab.remove();
        this.users.remove();
    }

    update() {
        if (this.shortName === "home")
            return;

        var updateAfter = 2.5 * 1000;
        var refreshAfter = 45 * 1000; // forced
        var now = Date.now();

        if ((this.userListDirty && (now - this.lastUserListChange >= updateAfter)) || (now - this.lastUserListRefresh >= refreshAfter)) {
            this.chatMgr.rohbot.requestUserList(this.shortName);
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

    getCompletionNames(word: string) {
        word = word.toLowerCase();

        return this.userList.map(user => {
            return htmlUnescape(user.Name);
        }).filter(name => {
            return name.toLowerCase().indexOf(word) === 0;
        });
    }

    addHistory(data: ChatHistoryPacket) {
        var history = data.Lines;
        var i: number;

        if (!data.Requested) {
            this.history.empty();

            for (i = 0; i < history.length; i++) {
                this.addLine(history[i], false);
            }

            if (this.isActive())
                this.chatMgr.scrollToBottom();
        } else {
            var prevHeight = this.history[0].clientHeight;

            for (i = history.length - 1; i >= 0; i--) {
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
        var statusLine: StatusLine = {
            Type: "state",
            Date: Date.now(),
            Chat: this.shortName,
            State: "Client",
            Content: message
        };

        this.addLine(statusLine);
    }

    renderLine(line: HistoryLine) {
        var timeFmt = RohStore.get("time format");
        if (timeFmt == null) timeFmt = "12hr";

        var date = new Date(line.Date);

        var data: any = {
            DateTime: date.toISOString(),
            Message: line.Content
        };

        if (timeFmt !== "off")
            data.Time = Chat.formatTime(date, timeFmt);

        switch (line.Type) {
            case "chat": {
                var chatLine = <ChatLine>line;
                var senderClasses = "";

                if (chatLine.UserType === "RohBot")
                    senderClasses = "rohBot ";
                else if (chatLine.InGame)
                    senderClasses = "inGame ";
                else
                    senderClasses = "steam ";

                senderClasses += chatLine.SenderStyle;

                if (chatLine.UserType === "Steam" && chatLine.SenderId !== "0")
                    data.SteamId = chatLine.SenderId;

                data.Sender = chatLine.Sender;
                data.SenderClasses = senderClasses;
                data.Message = this.linkify(chatLine.Content);
                break;
            }

            case "state": {
                var stateLine = <StateLine>line;
                if (stateLine.State === "Client") {
                    if (stateLine.Chat !== "home")
                        data.Message = this.linkify(stateLine.Content);
                    break;
                }

                var style = t => t === "Steam" ? "steam " : "rohBot ";

                var stateData: any = {
                    Class: "state",
                    For: stateLine.For,
                    ForStyle: style(stateLine.ForType) + stateLine.ForStyle
                };

                if (stateLine.ForType === "Steam" && stateLine.ForId !== "0")
                    stateData.ForSteamId = stateLine.ForId;

                if (stateLine.By !== "") {
                    stateData.By = stateLine.By;
                    stateData.ByStyle = style(stateLine.ByType) + stateLine.ByStyle;

                    if (stateLine.ByType === "Steam" && stateLine.ById !== "0")
                        stateData.BySteamId = stateLine.ById;
                }

                switch (stateLine.State) {
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

                    case "Action":
                        stateData.Class = "action";
                        stateData.Content1 = this.linkify(stateLine.Content.substr(stateLine.For.length));
                        break;

                    default:
                        console.warn("unhandled state type", stateLine.State);
                        break;
                }

                data.Message = templates.statemessage.render(stateData);
                break;
            }

            default:
                console.error("unsupported line type", line);
                break;
        }

        return templates.message.render(data);
    }

    addLine(line: HistoryLine, prepend: boolean = false) {
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

    setUserList(userList: UserListUser[]) {
        var now = Date.now();

        this.userList = userList;
        this.userListDirty = false;
        this.lastUserListChange = now;
        this.lastUserListRefresh = now;

        this.renderUserList();
    }

    private applyStateLine(line: HistoryLine) {

        var chatLine = <ChatLine>line;
        var stateLine = <StateLine>line;

        // force refresh when an endpoint's status changes
        if (line.Type === "chat" && chatLine.SenderId === "0") {
            if (chatLine.Content.indexOf("Connected to") === 0 || chatLine.Content.indexOf("Lost connection to") === 0) {
                this.userListDirty = true;
                this.lastUserListChange = 0;
                this.lastUserListRefresh = 0;
            }

            return;
        }

        if (line.Type !== "state")
            return;

        switch (stateLine.State) {
            case "Enter":
                this.userList.push({
                    Name: stateLine.For,
                    UserId: stateLine.ForId,
                    Rank: "Member",
                    Avatar: "0000000000000000000000000000000000000000",
                    Status: stateLine.ForType === "RohBot" ? "" : "Online",
                    Playing: null,
                    Web: stateLine.ForType === "RohBot",
                    Style: ""
                });
                break;

            case "Banned":
                if (stateLine.ForType === "RohBot")
                    break;
            case "Left":
            case "Disconnected":
                this.userList = this.userList.filter(e => {
                    return e.UserId !== stateLine.ForId;
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
        this.userList = _.sortBy(this.userList, e => {
            /*var colorGroup;

            if (e.Web)
                colorGroup = "1";
            else if (e.Playing == "")
                colorGroup = "2";
            else
                colorGroup = "3";*/

            return e.Name.toLowerCase();
        });

        var userMap = u => {
            if (u.Avatar === "0000000000000000000000000000000000000000")
                u.Avatar = "fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb";

            if (u.Web)
                u.Avatar = false;
            else
                u.AvatarFolder = u.Avatar.substring(0, 2);

            if (u.Playing === null)
                u.Playing = false;

            if (u.Playing === "")
                u.Playing = "&nbsp;";

            if (u.Status === "")
                u.Status = "&nbsp;";

            if (u.Playing)
                u.Status = u.Playing;

            if (u.Playing)
                u.Class = "inGame";
            else if (u.Web)
                u.Class = "rohBot";
            else
                u.Class = "steam";

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
        return currentChat != null && currentChat.shortName === this.shortName;
    }

    private linkify(text: string) {
        // Put spaces infront of emoticons to terminate urls
        text = text.replace(/ː(\w+?)ː/g, " ː<img src=\"/economy/emoticon/$1\" alt=\":$1:\" class=\"emote\" title=\"$1\">");
        text = text.replace(/((?:https?|ftps?|steam):\/\/\S+)/gi, "<a href=\"$1\" rel=\"noreferrer\" target=\"_blank\">$1</a>");
        text = text.replace(/(^|[\s,;"'])([A-Za-z0-9\-.:]+?\.(?:com|net|org|uk|edu|gov|biz|me|ch|tv|io|ca|nz)(?:[\/:]\S*|\b))/gi, "$1<a href=\"http://$2\" target=\"_blank\">$2</a>");
        text = text.replace("\n", "<br>");
        text = text.replace(/\ ː/g, ""); // Get rid of the sentinel chars. (triangle colons are guaranteed to never appear in normal text)
        return text;
    }

    static formatTime(date: Date, timeFmt: string) {
        if (timeFmt === "off")
            return "";

        var hours: any = date.getHours();
        var minutes: any = date.getMinutes();
        var military = timeFmt === "24hr";
        var suffix = "";

        if (military) {
            if (hours < 10)
                hours = "0" + hours;
        } else {
            suffix = " AM";
            if (hours >= 12) {
                suffix = " PM";
                hours -= 12;
            }

            if (hours === 0)
                hours = 12;
        }

        if (minutes < 10)
            minutes = "0" + minutes;

        suffix += "&nbsp;&#8209;&nbsp;";
        return hours + ":" + minutes + suffix;
    }
}
