// router.js
// Content-Type based URI router for WelsonJS framework
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
//
function RouteModel(path, callback) {
    this.path = path;
    this.callback = callback;
}

function RouterObject() {
    var routes = [];
    var renders = [];

    this.setRender = function(callback, contentType) {
        var contentType = (typeof contentType !== "undefined" ?
            contentType : ""
        );

        renders.push({
            "contentType": contentType,
            "callback": callback
        });
    };

    this.render = function(uri, data) {
        // Use expression: data:application/json,%7B%22key%22%3A%22value%22%7D
        // Use expression: data:text/html,%3Ch1%3EHello%3C%2Fh1%3E
        // Use expression: /path/to/file.html
        var contents = (function(uri, start, end) {
            if (start > -1 && end > start) {
                return [uri.substring(start, end), uri.substring(end + 1)];
            }
            
            return ["", uri];
        })(uri, uri.indexOf("data:"), uri.indexOf(','));

        var contentType = contents[0];
        var rawData = (contentType != "" ?
            decodeURIComponent(contents[1]) : contents[1]
        );
        
        // Multiple renderers are allowed for the same content type. They are executed in the order they were registered.
        renders.forEach(function(x) {
            if (x.contentType == contentType) {
                try {
                    if (typeof x.callback === "function") {
                        x.callback(rawData, data);
                    } else {
                        console.warn("Failed to render content of type " + x.contentType, "Not a function");
                    }
                } catch (e) {
                    console.warn("Failed to render content of type " + x.contentType, e.message);
                }
            }
        });
    };
    
    this.add = function(path, callback) {
        routes.push(new RouteModel(path, callback));
    };
    
    this.go = function(uri) {
        var path = uri.split(/[?#]/)[0]; 

        var model = routes.find(function(x) {
            return path === x.path || path.indexOf(x.path + "/") === 0;
        });

        if (!model) {
            console.error("No matching route found for:", uri);
            return;
        }

        if (typeof model.callback !== "function") {
            console.error("Invalid callback for route:", model.path);
            return;
        }

        try {
            model.callback(this.render);
        } catch (e) {
            console.error("Error executing callback for route:", model.path, e.message);
        }
    };
}

exports.Router = new RouterObject();

exports.VERSIONINFO = "Content-Type based URI router (router.js) version 0.1.2";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
