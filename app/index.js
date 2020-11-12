////////////////////////////////////////////////////////////////////////
// index.js
////////////////////////////////////////////////////////////////////////
var CONFIG = require("lib/config");
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");
var HTTP = require("lib/http");
var SYS = require("lib/system");
var SHELL = require("lib/shell");
var LDPlayer = require("lib/ldplayer");
var NoxPlayer = require("lib/noxplayer");

var token, userId;
var apiUrl = CONFIG.readConfig("/ApiUrl").first().getText();

var servers = [];
var applications = [];

var pingtest = function() {
    for (var i = 0; i < servers.length; i++) {
        servers[i].entry.find("span.ping").text(SYS.ping(servers[i].data.ipaddress) + " ms");
    }
};

var getLocalApplications = function() {
    // todo
};

var getMyApplications = function() {
    var onSuccess = function(res) {
        xmlStrings.push('<?xml version="1.0" encoding="UTF-8"?>');
        xmlStrings.push("<StaticIP>");
        for (var i = 0; i < res.data.length; i++) {
            xmlStrings.push("<Item>");
            xmlStrings.push("<Name>" + res.data[i].name + "</Name>");
            xmlStrings.push("<UniqueID>" + res.data[i].unique_id + "</UniqueID>");
            for (var k = 0; k < servers.length; k++) {
                if (servers[k].data.id == res.data[i].server) {
                    xmlStrings.push("<IPAddress>" + servers[k].data.ipaddress + "</IPAddress>");
                    applications.push({
                        id: res.data[i].id,
                        name: res.data[i].name,
                        uniqueId: res.data[i].unique_id,
                        server: res.data[i].server,
                        ipAddress: servers[k].data.ipaddress,
                        createdBy: userId
                    });
                }
            }
            xmlStrings.push("</Item>");
        }
        xmlStrings.push("</StaticIP>");

        FILE.writeFile("staticip.xml", xmlStrings.join("\r\n"), "utf-8");

        getLocalApplications();
    };

    HTTP.create()
        .setContentType("application/x-www-form-urlencoded")
        .setBearerAuth(token)
        .setUseCache(false)
        .get(apiUrl + "/netsolid/items/applications", onSuccess)
    ;
};

var getMyServers = function(assignedServers) {
    var onSuccess = function(res) {
        var template = $("#listview_servers .template");

        for (var i = 0; i < res.data.length; i++) {
            if (assignedServers.indexOf(res.data[i].id) > -1) {
                var entry = template.clone();
                entry.find("a.title").text(res.data[i].ipaddress);
                entry.find("div.description").text(res.data[i].name);
                entry.appendTo("#listview_servers");
                
                servers.push({
                    "data": res.data[i],
                    "entry": entry
                });
            }
        }

        template.css("display", "none");
    }

    HTTP.create()
        .setContentType("application/x-www-form-urlencoded")
        .setBearerAuth(token)
        .setUseCache(false)
        .get(apiUrl + "/netsolid/items/servers", onSuccess)
    ;

    pingtest();
    setInterval(pingtest, 5000);
    document.getElementById("btn_pingtest").onclick = pingtest;

    getMyApplications();
};

var getAssignedServers = function() {
    var onSuccess = function(res) {
        var assignedServers = [];
        for (var i = 0; i < res.data.length; i++) {
            if (res.data[i].assigned_to == userId) {
                assignedServers.push(res.data[i].server);
            }
        }
        getMyServers(assignedServers);
    };

    HTTP.create()
        .setContentType("application/x-www-form-urlencoded")
        .setBearerAuth(token)
        .setUseCache(false)
        .get(apiUrl + "/netsolid/items/assignedservers", onSuccess)
    ;
};

/*
var getMyServers = function() {
    getAssignedServers();
    
    var onSuccess = function(res) {
        var template = $("#listview_servers .template");

        for (var i = 0; i < res.data.length; i++) {
            if (assignedServers.indexOf(res.data[i].id) > -1) {
                var entry = template.clone();
                entry.find("a.title").text(res.data[i].ipaddress);
                entry.find("div.description").text(res.data[i].name);
                entry.appendTo("#listview_servers");
                
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
                var responseTime = SYS.ping(servers[i].data.ipaddress);
                servers[i].entry.find("span.ping").text(responseTime + " ms");
            }
        };
        document.getElementById("btn_pingtest").onclick = pingTest;
        setInterval(pingTest, 5000);
        pingTest();

        showApplications();

        document.getElementById("btn_assign").onclick = function() {
            showApplications();
            assignStaticIP();
        };
    };
    
    return;

    HTTP.create()
        .setHeaders({
            "Content-Type": "application/x-www-form-urlencoded",
            "Authorization": "Bearer " + token
        })
        .get(apiUrl + "/netsolid/items/servers", onSuccess)
    ;
};
*/

if (FILE.fileExists("token.txt")) {
    token = FILE.readFile("token.txt", "utf-8");
}

if (FILE.fileExists("userid.txt")) {
    userId = FILE.readFile("userid.txt", "utf-8");
}

if (typeof(token) !== "undefined") {
    OldBrowser.setContent(FILE.readFile("app\\servers.html", "utf-8"));
    getAssignedServers();
} else {
    OldBrowser.setContent(FILE.readFile("app\\login.html", "utf-8"));

    document.getElementById("loginform").onsubmit = function(ev) {
        ev.preventDefault();
    };

    document.getElementById("btn_submit").onclick = function() {
        var credential = {
            "email": document.getElementById("txt_email").value,
            "password": document.getElementById("txt_password").value
        };

        HTTP.create()
            .setContentType("application/json")
            .setRequestBody(credential)
            .post(apiUrl + "/netsolid/auth/authenticate", function(res) {
                if ("error" in res) {
                    console.error(res.error.message);
                } else if ("data" in res) {
                    console.log("ok");
                    FILE.writeFile("token.txt", res.data.token, "utf-8");
                    FILE.writeFile("userid.txt", res.data.user.id, "utf-8");

                    window.location.reload();
                }
            })
        ;
    };
}
