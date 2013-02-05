var http 	= require("http"),
	fs 		= require("fs");

exports.WebServer = function WebServer(port, dir) {
	var _this = this;
	
	var m_port = port;
	var m_dir = dir;
	var m_app = null;

	_this.start = function () {
		m_app = http.createServer(function (req, res) {
			if (req.url == "/")
				req.url = "/index.htm";

			fs.readFile(m_dir + req.url, function (err, data) {
				res.writeHead(200, {"Content-Type": "text/html"});

				if (err)
					data = "" + err;
				
				res.end(data);
			});
		});

		m_app.listen(m_port);

		console.log("Static file server is running on port " + m_port);
	};

	_this.stop = function () {
		if (m_app != null) {
			m_app.close();
		}
	};
	
	_this.app = function() {
		return m_app;
	};
}
