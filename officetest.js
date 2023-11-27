var Office = require("lib/msoffice");
var ChatGPT = require("lib/chatgpt");

function main(args) {
	var excel = new Office.Excel();
	
	excel.open();

	var message = ChatGPT.chat("Say this is a test!");
	console.log(message);

	excel.setValueByPosition(1, 1, message);

	//excel.close();
}

exports.main = main;