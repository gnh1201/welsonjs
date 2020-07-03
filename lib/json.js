////////////////////////////////////////////////////////////////////////
// JSON API
////////////////////////////////////////////////////////////////////////
var scope = {
    VERSIONINFO: "JSON Module (json.js) version 0.1",
    global: global,
	require: global.require
};

scope.stringify = function(obj) {
    var items = [];
    var isArray = (function(_obj) {
        try {
            return (_obj instanceof Array);
        } catch (e) {
            return false;
        }
    })(obj);
    var _toString = function(_obj) {
        try {
            if (typeof(_obj) == "object") {
                return scope.stringify(_obj);
            } else {
                var s = String(_obj).replace(/"/g, '\\"');
                if (typeof(_obj) == "number" || typeof(_obj) == "boolean") {
                    return s;
                } else {
                    return '"' + s + '"';
                }
            }
        } catch (e) {
            return "null";
        }
    };

    for (var k in obj) {
        var v = obj[k];

        if (!isArray) {
            items.push('"' + k + '":' + _toString(v));
        } else {
            items.push(_toString(v));
        }
    }

    if (!isArray) {
        return "{" + items.join(",") + "}";
    } else {
        return "[" + items.join(",") + "]";
    }
};

scope.parse = function(jsonString) {
    return (new Function("return " + jsonString)());
};

return scope;