function RouterObject() {
    this.RouteModel = function(path, callback) {
        this.path = path;
        this.callback = callback;
    };
    this.Routes = [];
    this.render = function(filename, data) {
        console.warn(typeof data !== "undefined" ? "DATA EXISTS" :  "NO DATA");
        console.warn(filename + " cannot be rendered");
    };

    this.setRender = function(render) {
        this.render = render;
    }

    this.add = function(path, callback) {
        this.Routes.push(new this.RouteModel(path, callback));
    }

    this.go = function(path) {
        var model = this.Routes.find(function(x) {
            return (x.path == path);
        });

        if (typeof model !== "undefined") {
            try {
                model.callback(this.render);
            } catch (e) {
                console.error(e.message);
            }
        }
    }
}

exports.Router = new RouterObject();

exports.VERSIONINFO = "URI Router (router.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
