// Example: Crawling a title of websites

var FILE = require("lib/file");
var HTTP = require("lib/http");
var Punycode = require("lib/punycode");

function main() {
    var lines = [];

    var district = JSON.parse(FILE.readFile("data\\korea-administrative-district.json", "utf-8"));
    var districtData = district.data;

    var domains = splitLn(FILE.readFile("data\\domains.txt", "utf-8"));

    var digFrame = function(handler, domain, response) {
        var frameURLs = handler.getFrameURLs();

        if (frameURLs.length > 0) {
            frameURLs.forEach(function(x) {
                var _handler = HTTP.create("CURL")
                    .setUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.67 Safari/537.36")
                    .setIsFollowRedirect(true)
                    .open("GET", "http://" + Punycode.encode(domain) + "/" + x)
                    .send();
                response += _handler.responseBody;
                response = digFrame(_handler, domain, response);
            });
        }

        return response;
    };

    domains.forEach(function(domain) {
        var handler = HTTP.create("CURL")
            .setUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.67 Safari/537.36")
            .setIsFollowRedirect(true)
            .open("GET", "http://" + Punycode.encode(domain) + "/")
            .send();
        var response = handler.responseBody;

        response = digFrame(handler, domain, response);

        var pos = response.search(/[0-9]{3}-[0-9]{2}-[0-9]{5}/g);
        console.log("Position:", pos);
        var bizNo = '';
        if (pos > -1) {
            bizNo = response.substring(pos, pos + 12);
        }

        var a = response.indexOf("<title>");
        var b = response.indexOf("</title>", a + 7);
        var title = '';
        if (a > -1 && b > -1) {
            title = response.substring(a + 7, b);
        }

        var bizRegion = '';
        districtData.forEach(function(x) {
            for (var k in x) {
                var d = [k].concat(x[k]);
                d.forEach(function(a) {
                    var s = a.substring(0, 2);
                    if (domain.indexOf(s) > -1 || title.indexOf(s) > -1 || response.indexOf(s) > -1) {
                        bizRegion = k;
                    }
                });
            }

            if (bizRegion != '') return false;
        });

        var bizType = '';
        var bizForm = '';
        var bizNote = '';
        var row = [domain, title, bizNo, bizRegion, bizType, bizForm, bizNote];
        lines.push(row.join(":"));
    });

    FILE.appendFile("data\\matches.txt", lines.join("\r\n"), "utf-8");
}

exports.main = main;
