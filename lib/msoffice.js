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

// Example: new Office.Word()
function Excel() {
    this.Application = CreateObject("Excel.Application");
    this.SupportedFileTypes = FileTypes.Excel;
}

// Example: new Office.PowerPoint()
function PowerPoint() {
    this.Application = CreateObject("PowerPoint.Application");
    this.SupportedFileTypes = FileTypes.PowerPoint;
}

// Example: new Office.Word()
function Word() {
    this.Application = CreateObject("Word.Application");
    this.SupportedFileTypes = FileTypes.Word;
}

exports.Excel = Excel;
exports.PowerPoint = PowerPoint;
exports.Word = Word;

exports.VERSIONINFO = "Microsoft Office interface (msoffice.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
