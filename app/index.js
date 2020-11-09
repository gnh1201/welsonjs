////////////////////////////////////////////////////////////////////////
// WebPage
////////////////////////////////////////////////////////////////////////
var CONFIG = require("lib/config");
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");
var HTTP = require("lib/http");
var SYS = require("lib/system");
var SHELL = require("lib/shell");
var LDPlayer = require("lib/ldplayer");
var NoxPlayer = require("lib/noxplayer");

var apiUrl = CONFIG.readConfig("/ApiUrl").first().getText();
var token, userId;

var servers = [];
var applications = [];

var assignStaticIP = function() {
    SHELL.runWindow("cscript app.js shadow");
};

var showLocalApplications = function() {
    var localApplications = [];

    // LDPlayer
    var LDPList = LDPlayer.getList();
    for (var i = 0; i < LDPList.length; i++) {
        localApplications.push({
            name: "LDPlayer",
            uniqueId: LDPList[i].title
        });
    }

    // NoxPlayer
    var NoxPList = NoxPlayer.getList();
    for (var i = 0; i < NoxPList.length; i++) {
        localApplications.push({
            name: "NoxPlayer",
            uniqueId: NoxPList[i].hostname
        });
    }
    
    // Chrome (demo)
    localApplications.push({
        name: "Program",
        uniqueId: "chrome-10001.exe"
    });
    localApplications.push({
        name: "Program",
        uniqueId: "chrome-10002.exe"
    });
    localApplications.push({
        name: "Program",
        uniqueId: "chrome-10003.exe"
    });

    var template = $("#listview_applications .template");
    for (var i = 0; i < servers.length; i++) {
        template.find("select").append($("<option/>").attr({
            value: servers[i].data.id
        }).text(servers[i].data.ipaddress));
    }

    for (var i = 0; i < localApplications.length; i++) {
        var serverId = "";
        
        var entry = template.clone();
        entry.find("a.title").text(localApplications[i].uniqueId + " (" + localApplications[i].name + ")");
        entry.find("select").data("application-name", localApplications[i].name);
        entry.find("select").data("application-unique-id", localApplications[i].uniqueId);

        for (var k = 0; k < applications.length; k++) {
            if (applications[k].name == localApplications[i].name
                && applications[k].uniqueId == localApplications[i].uniqueId
                && applications[k].createdBy == userId)
            {
                entry.find("select").data("application-id", applications[i].id);
                serverId = applications[k].server;
                break;
            }
        }

        entry.find("select").change(function() {
            if ($(this).val() != "") {
                var data = {
                    "status": "published",
                    "name": $(this).data("application-name"),
                    "unique_id": $(this).data("application-unique-id"),
                    "created_by": userId,
                    "server": $(this).val()
                };
                var applicationId = $(this).data("application-id");

                var req, res;
                var onSuccess = function(res) {
                    if ("error" in res) {
                        console.error(res.error.message);
                    } else {
                        console.log("반영되었습니다.");
                    }
                };
                
                if (applicationId) {
                    req = $.ajax({
                        type: "PATCH",
                        url: apiUrl + "/netsolid/items/applications/" + applicationId,
                        data: JSON.stringify(data),
                        contentType: 'application/json-patch+json',
                        success: onSuccess
                    });
                } else {
                    req = HTTP.post(apiUrl + "/netsolid/items/applications", JSON.stringify(data), {
                        "Content-Type": "application/json"
                    });
                    res = JSON.parse(req.responseText);
                    onSuccess(res);
                }
            }
        }).val(serverId);
        entry.appendTo("#listview_applications");
    }

    template.css("display", "none");
};

var showApplications = function() {
    var xmlStrings = [];

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
            var responseTime = SYS.pingTest(servers[i].data.ipaddress);
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

if (FILE.fileExists("token.txt")) {
    token = FILE.readFile("token.txt", "utf-8");
}

if (FILE.fileExists("userid.txt")) {
    userId = FILE.readFile("userid.txt", "utf-8");
}

if (typeof(token) !== "undefined") {
    showServers();
    showLocalApplications();
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

        var req = HTTP.post(apiUrl + "/netsolid/auth/authenticate", JSON.stringify(credential), {
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
