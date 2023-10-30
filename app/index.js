// index.js
// The entrypoint on WelsonJS GUI envionment

var FILE = require("lib/file");
var SHELL = require("lib/shell");
var OldBrowser = require("lib/oldbrowser");
var Router = require("lib/router").Router;

// using jsrender
Router.setRender(function(filename, data) {
    var template = FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8);
    var tmpl = $.templates(template);
	OldBrowser.setContent(tmpl.render(data));
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
	window.test_start = function(test_id) {
		SHELL.show(["cscript", "app.js", "testloader", test_id]);
	};

	window.gui_check = function() {
		var text1 = SHELL.exec("echo hello world!");
		alert(text1);

		var text2 = require("lib/system").getOS();
		alert(text2);

		alert("모든 메시지가 정상적으로 보였다면 테스트에 성공한 것입니다.");
	};
	
    var data = JSON.parse(FILE.readFile("data/test-oss-20231030.json", FILE.CdoCharset.CdoUTF_8));
    render("app\\test.html", {
        "data": data
    });
});

// go
Router.go('/');
