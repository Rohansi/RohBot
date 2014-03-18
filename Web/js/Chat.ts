
class Chat {

    chatMgr: ChatManager;
    name: string;
    shortName: string;
    history: JQuery;
    tab: JQuery;

    constructor(chatMgr: ChatManager, name: string, shortName: string) {
        this.chatMgr = chatMgr;
        this.name = name;
        this.shortName = shortName;

        this.history = $(templates.history.render({ ShortName: shortName }));
        this.history.appendTo("#history").hide();

        this.tab = $(templates.tab.render({ Name: name, ShortName: shortName }));

        this.tab.click(e => {
            console.log("switch to", shortName);
            this.chatMgr.switchTo(this.shortName);
            return false;
        });

        this.tab.find(".tab-close").click(e => {
            console.log("close", shortName);
            this.chatMgr.rohbot.sendMessage(this.shortName, "/leave " + this.shortName);
            return false;
        });

        this.tab.appendTo("#tabs");
    }

    destroy() {
        this.history.remove();
        this.tab.remove();
    }

    addHistory(history: any[], requested: boolean) {
        if (!requested) {
            this.history.empty();

            for (var i = 0; i < history.length; i++) {
                this.addLine(history[i], false);
            }

            this.chatMgr.scrollToBottom();
        } else {
            var firstMsg = this.history.find(":first")[0];

            for (var i = history.length - 1; i >= 0; i--) {
                this.addLine(history[i], true);
            }

            // TODO: proper scrolling
            //this.history.scrollTop(firstMsg.offsetTop);
        }
    }

    addLine(line: any, prepend: boolean) {
        var date = new Date(line.Date * 1000);

        var data: any = {
            Time: this.formatTime(date),
            DateTime: date.toISOString(),
            Message: line.Content
        };

        switch (line.Type) {
            case "chat": {
                var senderClasses = "";

                if (line.UserType == "RohBot")
                    senderClasses = "rohBot " + line.SenderStyle;
                else if (line.InGame)
                    senderClasses = "inGame";

                data.Sender = line.Sender;
                data.SenderClasses = senderClasses;
                data.Message = this.linkify(line.Content);
                break;
            }

            case "state":
                break;

            default:
                console.error("unsupported line type", line);
                break;
        }

        this.addHtml(templates.message.render(data), prepend);
    }

    addHtml(html: string, prepend: boolean) {
        var historyElem = this.history[0];
        var atBottom = this.history.outerHeight() >= (historyElem.scrollHeight - historyElem.scrollTop - 32);

        if (prepend)
            this.history.prepend(html);
        else
            this.history.append(html);

        if (!prepend && atBottom)
            this.chatMgr.scrollToBottom();
    }

    private linkify(text: string) {
        // Put spaces infront of <s to stop urlize seizing them as urls
        text = text.replace(/ː(\w+?)ː/g, ' ː<img src="/economy/emoticon/$1" alt="$1" class="emote">');
        text = urlize(text, { target: "_blank" });
        text = text.replace('\n', ' <br>'); // whitespace infront of a <br> isn't noticable
        text = text.replace(/\ ː/g, ''); // Get rid of the sentinel chars. (triangle colons are guaranteed to never appear in normal text)
        return text;
    }

    private formatTime(date: Date) {
        var hours: any = date.getHours();
        var minutes: any = date.getMinutes();
        var military = RohStore.get("clock format") == "24hr";
        var suffix = "";

        if (military) {
            if (hours < 10)
                hours = '0' + hours;
        } else {
            suffix = "AM";
            if (hours >= 12) {
                suffix = "PM";
                hours -= 12;
            }

            if (hours == 0)
                hours = 12;

            if (hours < 10)
                hours = " " + hours;
        }

        if (minutes < 10)
            minutes = "0" + minutes;

        if (military)
            return hours + ":" + minutes;
        else
            return hours + ":" + minutes + " " + suffix;
    }
}
