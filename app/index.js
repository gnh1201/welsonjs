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

var assign = function() {
    SHELL.runWindow("cscript app.js shadow");
};

var pingtest = function() {
    for (var i = 0; i < servers.length; i++) {
        servers[i].entry.find("span.ping").text(SYS.ping(servers[i].data.ipaddress) + " ms");
    }
};

var getLocalApplications = function() {
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
    
    // Chrome
    localApplications.push({
        name: "Chrome",
        uniqueId: "John"
    });
    localApplications.push({
        name: "Chrome",
        uniqueId: "James"
    });
    localApplications.push({
        name: "Chrome",
        uniqueId: "Jasmine"
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
                    HTTP.create()
                        .setContentType("application/json-patch+json")
                        .setBearerAuth(token)
                        .setRequestBody(JSON.stringify(data))
                        .patch(apiUrl + "/netsolid/items/applications/" + applicationId, onSuccess)
                    ;
                } else {
                    HTTP.create()
                        .setContentType("application/json")
                        .setBearerAuth(token)
                        .setRequestBody(data)
                        .post(apiUrl + "/netsolid/items/applications", onSuccess)
                    ;
                }
            }
        }).val(serverId);

        entry.appendTo("#listview_applications");
    }

    template.css("display", "none");
};

var getMyApplications = function() {
    var onSuccess = function(res) {
        var xmlStrings = [];
        
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
                    OldBrowser.reload();
                }
            })
        ;
    };

    document.getElementById("btn_assign").onclick = assign;
}
