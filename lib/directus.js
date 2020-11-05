////////////////////////////////////////////////////////////////////////
// Directus API
///////////////////////////////////////////////////////////////////////

var CONFIG = require("lib/config");
var HTTP = require("lib/http");

exports.authenticate = function() {
    var apiUrl = CONFIG.readConfig("/Config/ApiUrl").first().text;
/*
    var http = HTTP.post(apiUrl + "/netsolid/auth/authenticate", JSON.stringify({
        "email": "admin@example.org",
        "password": "1234"
    }), {
        "Content-Type": "application/json"
    });
    
    console.log(http.responseBody);
    return http;
*/
};
