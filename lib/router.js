// router.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
function RouteModel(path, callback) {
    this.path = path;
    this.callback = callback;
}

function RouterObject() {
    var routes = [];

    this.render = function(filename, data) {};
    this.setRender = function(render) {
        this.render = render;
    };
    this.add = function(path, callback) {
        routes.push(new RouteModel(path, callback));
    };
    this.go = function(path) {
        var model = routes.find(function(x) {
            return (x.path == path);
        });

        if (typeof model !== "undefined") {
            //try {
                model.callback(this.render);
            //} catch (e) {
            //    console.error(path, e.message);
            //}
        }
    };
}

exports.Router = new RouterObject();

exports.VERSIONINFO = "URI Router (router.js) version 0.1.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
