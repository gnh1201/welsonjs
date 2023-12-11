var STD = require("lib/std");
var SYS = require("lib/system");
var FILE = require("lib/file");

// msoffice.js
var FileTypes = {};

// https://support.microsoft.com/en-au/office/file-formats-that-are-supported-in-excel-0943ff2c-6014-4e8d-aaea-b83d51d46247
FileTypes.Excel = [
    {"extension": [".xlsx"], "type": "Excel Workbook"},
    {"extension": [".xlsm"], "type": "Excel Macro-Enabled Workbook (code)"},
    {"extension": [".xlsb"], "type": "Excel Binary Workbook"},
    {"extension": [".xltx"], "type": "Template"},
    {"extension": [".xltm"], "type": "Template (code)"},
    {"extension": [".xls"], "type": "Excel 97- Excel 2003 Workbook"},
    {"extension": [".xlt"], "type": "Excel 97- Excel 2003 Template"},
    {"extension": [".xls"], "type": "Microsoft Excel 5.0/95 Workbook"},
    {"extension": [".xml"], "type": "XML Spreadsheet 2003"},
    {"extension": [".xml"], "type": "XML Data"},
    {"extension": [".xlam"], "type": "Excel Add-In"},
    {"extension": [".xla"], "type": "Excel 97-2003 Add-In"},
    {"extension": [".xlw"], "type": "Excel 4.0 Workbook"},
    {"extension": [".xlr"], "type": "Works 6.0-9.0 spreadsheet"},
    {"extension": [".prn"], "type": "Formatted Text (Space-delimited)"},
    {"extension": [".txt"], "type": "Text (Tab-delimited)"},
    {"extension": [".txt"], "type": "Text (Macintosh)"},
    {"extension": [".txt"], "type": "Text (MS-DOS)"},
    {"extension": [".txt"], "type": "Unicode Text"},
    {"extension": [".csv"], "type": "CSV (comma delimited)"},
    {"extension": [".csv"], "type": "CSV (Macintosh)"},
    {"extension": [".csv"], "type": "CSV (MS-DOS)"},
    {"extension": [".dif"], "type": "DIF"},
    {"extension": [".slk"], "type": "SYLK"},
    {"extension": [".dbf"], "type": "DBF 3, DBF 4"},
    {"extension": [".ods"], "type": "OpenDocument Spreadsheet"},
    {"extension": [".pdf"], "type": "PDF"},
    {"extension": [".xps"], "type": "XPS Document"},
    {"extension": [".wmf", ".emf"], "type": "Picture"},
    {"extension": [".bmp"], "type": "Bitmap"},
    {"extension": [".xls"], "type": "Microsoft Excel file formats"},
    {"extension": [".slk"], "type": "SYLK"},
    {"extension": [".dif"], "type": "DIF"},
    {"extension": [".txt"], "type": "Text (tab-delimited)"},
    {"extension": [".csv"], "type": "CSV (Comma-delimited)"},
    {"extension": [".rtf"], "type": "Formatted text (Space-delimited)"},
    {"extension": [".gif", ".jpg", ".doc", ".xls", ".bmp"], "type": "Embedded object"},
    {"extension": [".gif", ".jpg", ".doc", ".xls", ".bmp"], "type": "Linked object"},
    {"extension": [".emf"], "type": "Office drawing object"},
    {"extension": [".txt"], "type": "Text"},
    {"extension": [".mht", ".mhtml"], "type": "Single File Web Page"},
    {"extension": [".htm", ".html"], "type": "Web Page"}
];

// https://support.microsoft.com/en-au/office/file-formats-that-are-supported-in-powerpoint-252c6fa0-a4bc-41be-ac82-b77c9773f9dc
FileTypes.PowerPoint = [
    {"extension": [".pptx"], "type": "PowerPoint Presentation"},
    {"extension": [".pptm"], "type": "PowerPoint Macro-Enabled Presentation"},
    {"extension": [".ppt"], "type": "PowerPoint 97-2003 Presentation"},
    {"extension": [".pdf"], "type": "PDF Document Format"},
    {"extension": [".xps"], "type": "XPS Document Format"},
    {"extension": [".potx"], "type": "PowerPoint Design Templates"},
    {"extension": [".potm"], "type": "PowerPoint Macro-Enabled Design Template"},
    {"extension": [".pot"], "type": "PowerPoint 97-2003 Design Template"},
    {"extension": [".thmx"], "type": "Office Theme"},
    {"extension": [".ppsx"], "type": "PowerPoint Show"},
    {"extension": [".ppsm"], "type": "PowerPoint Macro-Enabled Show"},
    {"extension": [".pps"], "type": "PowerPoint 97-2003 Show"},
    {"extension": [".ppam"], "type": "PowerPoint Add-In"},
    {"extension": [".ppa"], "type": "PowerPoint 97-2003 Add-In"},
    {"extension": [".xml"], "type": "PowerPoint XML Presentation"},
    {"extension": [".mp4"], "type": "MPEG-4 Video"},
    {"extension": [".wmv"], "type": "Windows Media Video"},
    {"extension": [".gif"], "type": "GIF (Graphics Interchange Format)"},
    {"extension": [".jpg"], "type": "JPEG (Joint Photographic Experts Group) FileFormat"},
    {"extension": [".png"], "type": "PNG (Portable Network Graphics) Format"},
    {"extension": [".tif"], "type": "TIFF (Tag Image File Format)"},
    {"extension": [".bmp"], "type": "Device Independent Bitmap"},
    {"extension": [".wmf"], "type": "Windows Metafile"},
    {"extension": [".emf"], "type": "Enhanced Windows Metafile"},
    {"extension": [".rtf"], "type": "Outline/RTF"},
    {"extension": [".pptx"], "type": "PowerPoint Picture Presentation"},
    {"extension": [".pptx"], "type": "Strict Open XML Presentation"},
    {"extension": [".odp"], "type": "OpenDocument Presentation"},
    {"extension": [".mht", ".mhtml"], "type": "Single File Web Page"},
    {"extension": [".htm", ".html"], "type": "Web Page"}
];

// https://learn.microsoft.com/en-us/deployoffice/compat/office-file-format-reference#file-formats-that-are-supported-in-word
FileTypes.Word = [
    {"extension": [".doc"], "type": "Word 97-2003 Document"},
    {"extension": [".docm"], "type": "Word Macro-Enabled Document"},
    {"extension": [".docx"], "type": "Word Document"},
    {"extension": [".docx"], "type": "Strict Open XML Document"},
    {"extension": [".dot"], "type": "Word 97-2003 Template"},
    {"extension": [".dotm"], "type": "Word Macro-Enabled Template"},
    {"extension": [".dotx"], "type": "Word Template"},
    {"extension": [".htm", ".html"], "type": "Web Page"},
    {"extension": [".htm", ".html"], "type": "Web Page, Filtered"},
    {"extension": [".mht", ".mhtml"], "type": "Single File Web Page"},
    {"extension": [".odt"], "type": "OpenDocument Text"},
    {"extension": [".pdf"], "type": "PDF"},
    {"extension": [".rtf"], "type": "Rich Text Format"},
    {"extension": [".txt"], "type": "Plain Text"},
    {"extension": [".wps"], "type": "Works 6-9 Document"},
    {"extension": [".xml"], "type": "Word 2003 XML Document"},
    {"extension": [".xml"], "type": "Word XML Document"},
    {"extension": [".xps"], "type": "XPS Document"}
];

// EXAMPLE: new Office.Excel()
function Excel() {
    this.application = CreateObject("Excel.Application");
    this.version = this.application.Version;
    this.application.Visible = true;

    console.info("Microsoft Office Excel:", this.version);

    this.currentWorkbook = null;
    this.currentWorksheet = null;
    this.range = null;

    this.open = function(filename) {
        if (typeof filename !== "undefined") {
            // check type of the path
            if (filename.indexOf(":\\") < 0 && filename.indexOf(":/") < 0) {
                filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;  // get absolute path
            }
            if (FILE.fileExists(filename)) {
                console.warn("Found the file:", filename);
                this.application.Workbooks.Open(filename);
                this.currentWorkbook = this.application.ActiveWorkbook;
            } else {
                console.warn("File not exists!");
                this.currentWorkbook = this.application.Workbooks.Add();
            }
        } else {
            this.currentWorkbook = this.application.Workbooks.Add();
        }
        this.selectWorksheet(1);

        return this;
    };

    this.close = function() {
        try {
            this.currentWorksheet = null;
            this.currentWorkbook.Close();
            this.currentWorkbook = null;
            this.application.Quit();
            this.application = null;
        } catch (e) {
            this.currentWorksheet = null;
            this.currentWorkbook = null;
            this.application = null;
        }
    };

    this.saveAs = function(filename) {
        try {
            this.currentWorkbook.saveAs(filename);
            return FILE.fileExists(filename);
        } catch (e) {
            console.error("Could not save a file:", e.message);
            return false;
        }
    };

    this.selectWorksheet = function(idx) {
        if (idx == 0) {
            this.currentWorksheet = this.application.ActiveSheet;
        } else {
            this.currentWorksheet = this.currentWorkbook.Worksheets(idx);
        }
        
        return this;
    };

    this.getRange = function(range) {
        return new Excel.Range(this.currentWorksheet.Range(range));
    };

    this.getCell = function(row, col) {
        return new Excel.Cell(this.currentWorksheet.Cells(row, col));
    };
};
Excel.SupportedFileTypes = FileTypes.Excel;
Excel.Range = function(range) {
    this.range = range;
    this.getCell = function(row, col) {
        return new Excel.Cell(this.range.Cells(row, col));
    };
};
Excel.Cell = function(cell) {
    this.cell = cell;
    // EXAMPLE: excel.getCell(1, 3).setValue("Hello world!");
    this.setValue = function(value) {
        this.cell.Value = value;
    };
    this.getValue = function() {
        return this.cell.Value;
    };
    // EXAMPLE: excel.getCellByPosition(1, 3).setFormula("=SUM(A1:A2)");
    this.setFormula = function(formula) {
        if (formula.indexOf('=') != 0) {
            console.warn("Be careful because it may not be the correct formula.");
        }
        this.cell.Formula = formula;
    }
    this.getFormula = function() {
        return this.call.Formula;
    }
};

// EXAMPLE: new Office.PowerPoint()
function PowerPoint() {
    this.application = CreateObject("PowerPoint.Application");
}
PowerPoint.SupportedFileTypes = FileTypes.PowerPoint;

// EXAMPLE: new Office.Word()
function Word() {
    this.application = CreateObject("Word.Application");
}
Word.SupportedFileTypes = FileTypes.Word;

exports.Excel = Excel;
exports.PowerPoint = PowerPoint;
exports.Word = Word;

exports.VERSIONINFO = "Microsoft Office interface (msoffice.js) version 0.1.7";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
