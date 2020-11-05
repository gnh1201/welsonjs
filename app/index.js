////////////////////////////////////////////////////////////////////////
// WebPage
////////////////////////////////////////////////////////////////////////
var CONFIG = require("lib/config");
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");
var HTTP = require("lib/http");
var SYS = require("lib/system");

var apiUrl = CONFIG.readConfig("/Config/ApiUrl").first().text;
var token, userId;

var servers = [];

var getApplications = function() {
    var applications = [], xmlStrings = [];

    var req = HTTP.get(apiUrl + "/netsolid/items/applications", "", {
        "Content-Type": "application/x-www-form-urlencoded",
        "Authorization": "bearer " + token,
        //"Pragma": "no-cache",
        //"Cache-Control": "no-cache",
        "If-Modified-Since": "Sat, 1 Jan 2000 00:00:00 GMT"
    });
    var res = JSON.parse(req.responseText);

    xmlStrings.push('<?xml version="1.0" encoding="UTF-8"?>');
    xmlStrings.push("<StaticIP>");
    for (var i = 0; i < res.data.length; i++) {
        xmlStrings.push("<Item>");
        xmlStrings.push("<Name>" + res.data[i].name + "</Name>");
        xmlStrings.push("<UniqueID>" + res.data[i].unique_id + "</UniqueID>");
        for (var k = 0; k < servers.length; k++) {
            if (servers[k].data.id == res.data[i].server) {
                xmlStrings.push("<IPAddress>" + servers[k].data.ipaddress + "</IPAddress>");
            }
        }
        xmlStrings.push("</Item>");
    }
    xmlStrings.push("</StaticIP>");

    FILE.writeFile("staticip.xml", xmlStrings.join("\r\n"), "utf-8");
};

var getAssignedServers = function() {
    var assignedServers = [];

    var req = HTTP.get(apiUrl + "/netsolid/items/assignedservers", "", {
        "Content-Type": "application/x-www-form-urlencoded",
        "Authorization": "bearer " + token,
        //"Pragma": "no-cache",
        //"Cache-Control": "no-cache",
        "If-Modified-Since": "Sat, 1 Jan 2000 00:00:00 GMT"
    });
    
    var res = JSON.parse(req.responseText);

    for (var i = 0; i < res.data.length; i++) {
        if (res.data[i].assigned_to == userId) {
            assignedServers.push(res.data[i].server);
        }
    }

    return assignedServers;
};

var showServers = function() {
    OldBrowser.setContent(FILE.readFile("app\\servers.html", "utf-8"));

    var assignedServers = getAssignedServers();
    var req = HTTP.get(apiUrl + "/netsolid/items/servers", "", {
        "Content-Type": "application/x-www-form-urlencoded",
        "Authorization": "bearer " + token
    });
    var res = JSON.parse(req.responseText);
    var template = $("#listview .template");

    for (var i = 0; i < res.data.length; i++) {
        if (assignedServers.indexOf(res.data[i].id) > -1) {
            var entry = template.clone();
            entry.find("a.title").text(res.data[i].ipaddress);
            entry.find("div.description").text(res.data[i].name);
            entry.appendTo("#listview");
            
            servers.push({
                "data": res.data[i],
                "entry": entry
            });
        }
    }

    template.css("display", "none");

    document.getElementById("btn_logout").onclick = function() {
        if (FILE.fileExists("token.txt")) {
            token = FILE.deleteFile("token.txt")
        }
        
        if (FILE.fileExists("userid.txt")) {
            userId = FILE.deleteFile("userid.txt");
        }

        exit(0);
    };
    
    var pingTest = function() {
        for (var i = 0; i < servers.length; i++) {
            var responseTime = SYS.pingTest(servers[i].data.ipaddress);
            servers[i].entry.find("span.ping").text(responseTime + " ms");
        }
    };
    document.getElementById("btn_pingtest").onclick = pingTest;
    setInterval(pingTest, 5000);
    pingTest();

    getApplications();
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

            window.location.reload();
        }
    };
}
