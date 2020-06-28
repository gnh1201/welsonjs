/*
 * webloader.js
 */

var FILE = require('lib/file');

return {
    addScript: function(url, callback) {
        var el = document.createElement("script");
        el.src = url;
        el.type = "text/javascript";
        el.charset = "utf-8";
        document.head.appendChild(el);
        if(typeof(callback) === "function") {
            el.onload = callback(el);
        }
        return el;
    },
    addStylesheet: function(s, callback) {
        var el = document.createElement("link");
        el.href = url;
        el.rel = "stylesheet";
        el.type = "text/css";
        document.head.appendChild(el);
        if(typeof(callback) === "function") {
            el.onload = callback(el);
        }
        return el;
    },
    main: function() {
        var contents = FILE.readFile("app\\app.html", "utf-8");
        document.getElementById("app").innerHTML = contents;
        this.addScript("app/assets/js/jquery-3.5.1.min.js");
        this.addScript("app/assets/js/jquery.form.min.js");
        this.addScript("app/assets/js/index.js");
        return 0;
    }
}
