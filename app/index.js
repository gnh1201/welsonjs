////////////////////////////////////////////////////////////////////////
// WebPage
////////////////////////////////////////////////////////////////////////
var CONFIG = require("lib/config");
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");
var HTTP = require("lib/http");

var apiUrl = CONFIG.readConfig("/Config/ApiUrl").first().text;
var token, userId;

var showServers = function() {
    OldBrowser.setContent(FILE.readFile("app\\servers.html", "utf-8"));
    
    var req = HTTP.get(apiUrl + "/netsolid/items/assignedservers", "filter[assigned_to][eq]=" + userId, {
        "Content-Type": "application/x-www-form-urlencoded",
        "Authorization": "bearer " + token
    });
    
    alert(req.responseText);

    /*
    var res = JSON.parse(req.responseText);

    for(var i = 0; i < res.data.length; i++) {
        alert(res.data[i].assigned_to);
    }
    */
};


if (FILE.fileExists("token.txt")) {
    token = FILE.readFile("token.txt", "utf-8");
}

if (FILE.fileExists("userid.txt")) {
    userId = FILE.readFile("userid.txt", "utf-8");
}

if (typeof(token) !== "undefined") {
    showServers();
} else {
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
            FILE.writeFile("userid.txt", res.data.user.id, "utf-8");
            showServers();
        }
    };
}
