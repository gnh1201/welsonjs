// msoffice.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var STD = require("lib/std");
var SYS = require("lib/system");
var FILE = require("lib/file");
var FileTypes = require("lib/filetypes");

// EXAMPLE: new Office.Excel()
function Excel() {
    this.application = CreateObject("Excel.Application");
    this.version = this.application.Version;
    this.application.Visible = true;

    console.info("Microsoft Office Excel", this.version);

    this.currentWorkbook = null;
    this.currentWorksheet = null;
    this.range = null;

    this.open = function(filename) {
        if (typeof filename !== "undefined") {
            // check type of the path
            if (!FILE.isAbsolutePath(filename)) {
                filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;  // get absolute path
            }
            if (FILE.fileExists(filename)) {
                console.info("FOUND", filename);
                this.application.Workbooks.Open(filename);
                this.currentWorkbook = this.application.ActiveWorkbook;
            } else {
                console.warn("NOT FOUND", filename);
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
        
        // switch to the worksheet
        this.currentWorksheet.Activate();
        
        return this;
    };

    this.getRange = function(range) {
        return new Excel.Range(this.currentWorksheet.Range(range));
    };

    this.getCellByPosition = function(row, col) {
        return new Excel.Cell(this.currentWorksheet.Cells(row, col));
    };
};
Excel.Range = function(range) {
    this.range = range;
    this.getCellByPosition = function(row, col) {
        return new Excel.Cell(this.range.Cells(row, col));
    };
};
Excel.Cell = function(cell) {
    this.cell = cell;
    // EXAMPLE: excel.getCellByPosition(1, 3).setValue("Hello world!");
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
Excel.FileExtensions = FileTypes.getExtensionsByOpenWith("msexcel");

// EXAMPLE: new Office.PowerPoint()
function PowerPoint() {
    this.application = CreateObject("PowerPoint.Application");
    this.version = this.application.Version;
    this.application.Visible = true;

    console.info("Microsoft Office PowerPoint", this.version);

    this.currentPresentation = null;

    this.open = function(filename) {
        if (typeof filename !== "undefined") {
            // check type of the path
            if (filename.indexOf(":\\") < 0 && filename.indexOf(":/") < 0) {
                filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;  // get absolute path
            }
            if (FILE.fileExists(filename)) {
                console.info("FOUND", filename);
                this.application.Presentations.Open(filename);
                this.currentPresentation = this.application.ActivePresentation;
            } else {
                console.warn("NOT FOUND", filename);
                this.currentPresentation = this.application.Presentations.Add(true);
            }
        } else {
            this.currentPresentation = this.application.Presentations.Add(true);
        }
        //this.selectPresentation(1);
    };

    this.selectPresentation = function(idx) {
        if (idx == 0) {
            this.currentPresentation = this.application.ActivePresentation;
        } else {
            this.currentPresentation = this.application.Presentations(idx);
        }
        return this;
    };

    this.saveAs = function(filename) {
        try {
            this.currentPresentation.saveAs(filename);
            return FILE.fileExists(filename);
        } catch (e) {
            console.error("Could not save a file:", e.message);
            return false;
        }
    };

    this.close = function() {
        try {
            this.currentPresentation.Close()
            this.currentPresentation = null;
            this.application = null;
        } catch (e) {
            this.currentPresentation = null;
            this.application = null;
        }
    };

    // get all slides
    this.getSlides = function() {
        var slides = [];

        var slideEnum = new Enumerator(this.currentPresentation.Slides);
        for (; !slideEnum.atEnd(); slideEnum.moveNext()) {
            slides.push(new PowerPoint.Slide(slideEnum.item()));
        }

        return slides;
    };
}
PowerPoint.Slide = function(slide) {
    this.slide = slide;

    this.getShapes = function() {
        // todo
    };
};
PowerPoint.Shape = function(shape) {
    this.shape = shape;
}
PowerPoint.FileExtensions = FileTypes.getExtensionsByOpenWith("msppt");

// EXAMPLE: new Office.Word()
function Word() {
    this.application = CreateObject("Word.Application");
    this.version = this.application.Version;
    this.application.Visible = true;

    console.info("Microsoft Office Word", this.version);
    
    this.open = function(filename) {
        if (typeof filename !== "undefined") {
            // check type of the path
            if (filename.indexOf(":\\") < 0 && filename.indexOf(":/") < 0) {
                filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;  // get absolute path
            }
            if (FILE.fileExists(filename)) {
                console.info("FOUND", filename);
            } else {
                console.warn("NOT FOUND", filename);
            }
        }
    };
}
Word.FileExtensions = FileTypes.getExtensionsByOpenWith("msword");

exports.Excel = Excel;
exports.PowerPoint = PowerPoint;
exports.Word = Word;

exports.VERSIONINFO = "Microsoft Office interface (msoffice.js) version 0.2.2";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
