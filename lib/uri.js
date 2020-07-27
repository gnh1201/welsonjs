//////////////////////////////////////////////////////////////////////////////////
//
//    uri.js
//
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

exports.VERSIONINFO = "URI Lib (uri.js) version 0.1";
exports.global = global;
exports.require = global.require;

/////////////////////////////////////////////////////////////////////////////////
// exports.parseQueryString
/////////////////////////////////////////////////////////////////////////////////

exports.parseQueryString = function(queryString) {
    var query = {};
    var pairs = [];

    if(queryString.substring(0, 1) == '?') {
        pairs = queryString.substring(1).split('&');
    } else {
        pairs = queryString.split('&');
    }

    for (var i = 0; i < pairs.length; i++) {
        var pair = pairs[i].split('=');
        var _k = decodeURIComponent(pair[0]);
        var _v = decodeURIComponent(pair[1] || '');
        var path = _k.split('[').map(function(s) {
            return (s.indexOf(']') < 0 ? s : s.substring(0, s.length -1));
        }).join('/');

        if(path in query) {
            if (Array.isArray(query[path])) {
                query[path].push(_v);
            } else {
                query[path] = [query[path], _v];
            }
        } else {
            query[path] = _v;
        }
    };

    return query;
};
