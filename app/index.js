// index.js - The entrypoint on WelsonJS GUI envionment
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
var FILE = require("lib/file");
var SHELL = require("lib/shell");
var Browser = require("lib/browser");
var Router = require("lib/router").Router;

// using jsrender
Router.setRender(function(filename, data) {
    var template = FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8);
    var tmpl = $.templates(template);
    Browser.setContent(tmpl.render(data));

    // check all links
    Object.values(document.getElementsByTagName("a")).map(function(x) {
        x.addEventListener("click", function(e) {
            var href = e.target.getAttribute("href");    // NOTE: x.href != x.getAttribute("href")
            if (href.indexOf('/') == 0) {
                e.preventDefault();
                Router.go(href);
            }
        });
    });
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

    var content = FILE.readFile("data/test-oss-20231030.json", FILE.CdoCharset.CdoUTF_8);
    var data = JSON.parse(content);
    render("app/test.html", {
        "data": data
    });
});

// nodepad
Router.add('/notepad', function(render) {
    // load resources
    Browser.addStylesheet("app/assets/mixed/summernote-0.8.21-proposal/summernote-lite.css");
    Browser.waitUntil(function(test, ttl) {
        Browser.addScript("app/assets/mixed/summernote-0.8.21-proposal/summernote-lite.js", function(el) {
            // set DOM id
            var target_dom_id = "summernote";

            // load HTML
            render("app/notepad.html", {
                "target_dom_id": target_dom_id
            });

            // load Summernote (wysiwyg editor)
            $('#' + target_dom_id).summernote({
                minHeight: 300
            });

        }, test, ttl);
    }, function(el) {
        return $.summernote;
    });

	document.getElementById("useragent").innerHTML = window.navigator.userAgent;
});

// go
Router.go('/');
