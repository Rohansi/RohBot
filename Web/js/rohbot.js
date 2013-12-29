/* jshint jquery: true */
$(document).ready(function() { 'use strict';
	window.initializeRohBot();

	$("#password").keydown(function(e) {
		if (e.keyCode === 13) {
			$("#password").blur().focus();
			$("#loginButton").click();
			return false;
		}
	});
});
