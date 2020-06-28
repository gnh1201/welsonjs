/*
 * index.js
 */

var LIB = require('lib/std');
//var DB = require('lib/db');

return { 
	main: function() {
		console.log("welcome index.js");
		
		document.getElementById("click1").onclick = function() {
		    alert("hello world");
		};
		
		return 0;
	}
}
