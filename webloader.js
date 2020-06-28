/*
 * webloader.js
 */

var FILE = require('lib/file');

return {
	main: function() {
        var contents = FILE.readFile("app\\app.html");
        document.getElementById("app").innerHTML = contents;
		return 0;
	}
}
