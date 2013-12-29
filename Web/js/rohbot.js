
var storage = localStorage;//window.rohStore;

var roomName;
var requestedHistory;
var oldestMessage;

$(document).ready(function() {

	$("#password").keydown(function(e) {
		if (e.keyCode == 13) {
			$("#password").blur().focus();
			$("#loginButton").click();
			return false;
		}
	});
	
	$("#loginButton").click(function() {
		rohbot.login({
			Username: $("#username").val(),
			Password: $("#password").val()
		});
	});
	
	$("#registerButton").click(function() {
		rohbot.register($("#username").val(), $("#password").val());
	});
	
	$("#chat").scroll(function() {
		if ($("#chat").scrollTop() == 0 && !requestedHistory) {
			rohbot.requestHistory(oldestMessage);
			requestedHistory = true;
		}
	});
	
	$(window).resize(function() {
		$("#chat").scrollTop($("#chat")[0].scrollHeight);
	});
});
