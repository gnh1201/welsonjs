var FILE = require("lib/file");
var HTTP = require("lib/http");
var CONFIG = require("lib/config");

exports.checkUpdates = function() {
    var token = FILE.readFile("token.txt", "utf-8");
    var apiUrl = CONFIG.getValue("ApiUrl");
    var onSuccess = function(res) {
        var data = res.data;
        for (var i = 0; i < data.length; i++) {
            console.log("UPDATE ID: ", data.file);
        }
    }

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
};