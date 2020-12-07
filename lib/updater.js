var FILE = require("lib/file");
var HTTP = require("lib/http");
var CONFIG = require("lib/config");
var SHELL = require("lib/shell");

var apiUrl = CONFIG.getValue("ApiUrl");
var token = null;
var versionId = 0;

if (FILE.fileExists("token.txt")) {
    token = FILE.readFile("token.txt", "utf-8");
}

if (FILE.fileExists("versionid.txt")) {
    versionId = parseInt(FILE.readFile("versionid.txt", "utf-8"));
}

var downloadFile = function(url) {
    // download a file
    console.log(SHELL.exec(["bin\\curl", url, "-o", "update.zip"]));

    // extract a file
    console.log(SHELL.exec(["bin\\unzip", "-o", "update.zip"]));

    // run installer
    SHELL.run(["start", "mysetup.exe"]);

    // close window
    exit();
};

var requestFile = function(id) {
    var onSuccess = function(res) {
        var data = res.data;
        var full_url = data.data.full_url;

        console.log("Downloading a file...");
        downloadFile(full_url);
    };

    console.log("Requesting a file...");
    HTTP.create()
        .setContentType("application/x-www-form-urlencoded")
        .setBearerAuth(token)
        .get(apiUrl + "/netsolid/files/" + id, onSuccess)
    ;
};

var checkUpdates = function() {
    if (!token) {
        console.warn("Could not find authorization token. 로그인이 필요합니다.");
    } else {
        var onSuccess = function(res) {
            var data = res.data;
            for (var i = 0; i < data.length; i++) {
                if (data[i].id > versionId) {
                    console.info("업데이트가 있습니다. 다운로드를 진행합니다.");
                    requestFile(data[i].file);
                }
            }
        };

        HTTP.create()
            .setContentType("application/x-www-form-urlencoded")
            .setBearerAuth(token)
            .setParameters({
                "status": "published",
                "sort": "-created_on",
                "limit": 1
            })
            .get(apiUrl + "/netsolid/items/updates", onSuccess)
        ;
    }
};

exports.checkUpdates = checkUpdates;
