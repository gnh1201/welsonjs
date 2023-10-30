// index.js
// The entrypoint on WelsonJS GUI envionment

var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");
var Router = require("lib/router").Router;

// using jsrender
Router.setRender(function(filename, data) {
	var template = OldBrowser.setContent(FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8));
	var tmpl = $.templates(template);
	return tmpl.render(data);
});

// main
Router.add('/', function(render) {
	render("app\\signin.html", {});

	var token;
	if (FILE.fileExists("token.txt")) {
		token = FILE.readFile("token.txt", FILE.CdoCharset.CdoUTF_8);
	}

	document.getElementById("loginform").onsubmit = function(ev) {
		ev.preventDefault();
	};

	if (FILE.fileExists("credential.json")) {
		var credential = JSON.parse(FILE.readFile("credential.json", FILE.CdoCharset.CdoUTF_8));
		document.getElementById("txt_email").value = credential.email;
		document.getElementById("txt_password").value = credential.password;
	}

	document.getElementById("btn_submit").onclick = function() {
		var credential = {
			"email": document.getElementById("txt_email").value,
			"password": document.getElementById("txt_password").value
		};
		
		FILE.writeFile("credential.json", JSON.stringify(credential), FILE.CdoCharset.CdoUTF_8);
	};
});

// test
Router.add('/test', function(render) {
	var profile = JSON.parse(FILE.readFile("data/test-oss-20231030.json", FILE.CdoCharset.CdoUTF_8));

	render("app\\test.html", {
		"profile": profile
	});
});

// go
Router.go('/');
