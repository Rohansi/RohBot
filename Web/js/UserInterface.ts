
class UserInterface {

    rohbot: RohBot;
    chatMgr: ChatManager;

    onLogin: (username: string, password: string) => void;
    onRegister: (username: string, password: string) => void;

    constructor(rohbot: RohBot, chatMgr: ChatManager) {
        this.rohbot = rohbot;
        this.chatMgr = chatMgr;

        var username = $("#username");
        var password = $("#password");
        var loginBtn = $("#loginButton");
        var registerBtn = $("#registerButton");

        loginBtn.click(e => {
            this.rohbot.login(username.val(), password.val());
            e.preventDefault();
        });

        registerBtn.click(e => {
            this.rohbot.register(username.val(), password.val());
            e.preventDefault();
        });

        var send = $("#send");
        var messageBox = $("#message-box");

        send.click(e => {
            var currentChat = chatMgr.getCurrentChat();

            // TODO: client commands

            rohbot.sendMessage(currentChat.shortName, messageBox.val());
            e.preventDefault();
        });
    }

    setChatEnabled(value: boolean) {
        if (value) {
            $("#header").hide();
            $("#message-box").attr("disabled", "false").val("");
        } else {
            $("#header").show();
            $("#message-box").attr("disabled", "true").val("Guests cannot speak!");
        }
    }

}
