////////////////////////////////////////////////////////////////////////
// WebPage
////////////////////////////////////////////////////////////////////////
var CONFIG = require("lib/config");
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");
var HTTP = require("lib/http");

var apiUrl = CONFIG.readConfig("/Config/ApiUrl").first().text;
var token;

if (FILE.fileExists("token.txt")) {
    token = FILE.readFile("token.txt", "utf-8");
}

if (typeof(token) === "undefined") {
    OldBrowser.setContent(FILE.readFile("app\\login.html", "utf-8"));

    document.getElementById("loginform").onsubmit = function(ev) {
        ev.preventDefault();
    };

    document.getElementById("btn_submit").onclick = function() {
        var credential = JSON.stringify({
            "email": document.getElementById("txt_email").value,
            "password": document.getElementById("txt_password").value
        });

        var req = HTTP.post(apiUrl + "/netsolid/auth/authenticate", credential, {
            "Content-Type": "application/json"
        });

        var res = JSON.parse(req.responseText);
        
        if ("error" in res) {
            console.error(res.error.message);
        } else if ("data" in res) {
            console.log("ok");
            FILE.writeFile("token.txt", res.data.token, "utf-8");
        }
    };
}
