
var requestedHistory;
var oldestMessage;

$(document).ready(function() {
	initializeRohBot();

	$("#password").keydown(function(e) {
		if (e.keyCode == 13) {
			$("#password").blur().focus();
			$("#loginButton").click();
			return false;
		}
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
