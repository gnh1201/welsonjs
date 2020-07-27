//////////////////////////////////////////////////////////////////////////////////
//
//	excel.js
//
/////////////////////////////////////////////////////////////////////////////////

var FILE = require("lib/file");

exports.createExcelFile = function(filename) {
    var success = false;

    try {
        var objExcel = CreateObject("Excel.Application");
        objExcel.visible = true;

        var objWorkbook = objExcel.workbooks.add();

        if(!FILE.fileExists(filename)) {
            objWorkbook.saveAs(filename);
            success = FILE.fileExists(filename);
        }

        objWorkbook.close();
        objExcel.quit();
    } catch(e){}

    return success;
};

exports.openExcelFile = function(filename, callback) {
    var success = false;

    if(FILE.fileExists(filename)) {
        try {
            var objExcel = CreateObject("Excel.Application");
            objExcel.visible = true;
            var objWorkbook = objExcel.workbooks.open(filename);

            if (typeof(callback) !== "undefined") {
                success = callback(objWorkbook);
            } else {
                success = true;
            }

            objWorkbook.close();
            objExcel.quit();
        } catch(e){}
    }
    
    return success;
};
