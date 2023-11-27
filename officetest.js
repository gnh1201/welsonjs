var Office = require("lib/msoffice");

function main(args) {
	var excel = new Office.Excel();
	
	excel.open();
	
	excel.setValueByCell(1, 1, "hello world");
	
	excel.close();
}

exports.main = main;