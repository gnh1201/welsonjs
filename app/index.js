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
var apiUrl = CONFIG.getValue("ApiUrl");

var servers = [];
var applications = [];
var localApplications = [];

var assign = function() {
    SHELL.runVisibleWindow("cscript app.js shadow");
};

var pingtest = function() {
    for (var i = 0; i < servers.length; i++) {
        servers[i].entry.find("span.ping").text(SYS.ping(servers[i].data.ipaddress) + " ms");
    }
};

var getLocalApplications = function() {
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
    /*
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
    */

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
                entry.find("select").data("application-id", applications[k].id);
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
            if (res.data[i].created_by == userId) {
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

                // for Chrome
                if (res.data[i].name == "Chrome") {
                    localApplications.push({
                        name: "Chrome",
                        uniqueId: res.data[i].unique_id
                    });
                }
            }
        }
        xmlStrings.push("</StaticIP>");

        FILE.writeFile("staticip.xml", xmlStrings.join("\r\n"), "utf-8");

        getLocalApplications();
    };

    HTTP.create()
        .setContentType("application/x-www-form-urlencoded")
        .setBearerAuth(token)
        .setUseCache(false)
        .setParameters({
            "filter[created_by][eq]": userId
        })
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
        .setParameters({
            "limit": "-1"
        })
        .get(apiUrl + "/netsolid/items/servers", onSuccess)
    ;

    pingtest();
    setInterval(pingtest, 5000);
    //document.getElementById("btn_pingtest").onclick = pingtest;

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
        .setParameters({
            "filter[assigned_to][eq]": userId
        })
        .get(apiUrl + "/netsolid/items/assignedservers", onSuccess)
    ;
};

var getNotices = function() {
    var onSuccess = function(res) {
        var template = $("#listview_notices .template");

        for (var i = 0; i < res.data.length; i++) {
            var entry = template.clone();
            entry.find("a.title").text(res.data[i].title);
            entry.find("div.description").text(res.data[i].content);
            entry.find("span.ping").text(res.data[i].created_on.substring(0, 10));
            entry.appendTo("#listview_notices");
        }

        template.css("display", "none");
    };

    HTTP.create()
        .setContentType("application/x-www-form-urlencoded")
        .setParameters({
            "sort": "-created_on",
            "limit": 3
        })
        .setBearerAuth(token)
        .setUseCache(false)
        .get(apiUrl + "/netsolid/items/notices", onSuccess)
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
    getNotices();

    document.getElementById("btn_logout").onclick = function() {
        if (FILE.fileExists("token.txt")) {
            token = FILE.deleteFile("token.txt")
        }
        
        if (FILE.fileExists("userid.txt")) {
            userId = FILE.deleteFile("userid.txt");
        }

        exit(0);
    };

    document.getElementById("btn_refresh").onclick = function() {
        OldBrowser.reload();
    };

    document.getElementById("btn_close").onclick = function() {
        OldBrowser.close();
    };

    document.getElementById("version").innerHTML = "현재 버전: " + CONFIG.getValue("Version");

    assign();
} else {
    OldBrowser.setContent(FILE.readFile("app\\login.html", "utf-8"));

    document.getElementById("loginform").onsubmit = function(ev) {
        ev.preventDefault();
    };

    if (FILE.fileExists("credential.json")) {
        var credential = JSON.parse(FILE.readFile("credential.json", "utf-8"));
        document.getElementById("txt_email").value = credential.email;
        document.getElementById("txt_password").value = credential.password;
    }

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
                    FILE.writeFile("credential.json", JSON.stringify(credential), "utf-8");
                    FILE.writeFile("token.txt", res.data.token, "utf-8");
                    FILE.writeFile("userid.txt", res.data.user.id, "utf-8");
                    OldBrowser.reload();
                }
            })
        ;
    };
}
