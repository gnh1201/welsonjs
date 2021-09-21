////////////////////////////////////////////////////////////////////////
// index.js
////////////////////////////////////////////////////////////////////////

var token;
if (FILE.fileExists("token.txt")) {
    token = FILE.readFile("token.txt", "utf-8");
}

OldBrowser.setContent(FILE.readFile("app\\signin.html", "utf-8"));

document.getElementById("loginform").onsubmit = function(ev) {
	ev.preventDefault();
};

if (FILE.fileExists("credential.json")) {
	var credential = JSON.parse(FILE.readFile("credential.json", "utf-8"));
	document.getElementById("txt_email").value = credential.email;
	document.getElementById("txt_password").value = credential.password;
}

document.getElementById("btn_submit").onclick = function() {
	var credential = {
		"email": document.getElementById("txt_email").value,
		"password": document.getElementById("txt_password").value
	};
	
	FILE.writeFile("credential.json", JSON.stringify(credential), "utf-8");
};
