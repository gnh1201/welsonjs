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
            if (!FILE.isAbsolutePath(filename)) {
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
            if (!FILE.isAbsolutePath(filename)) {
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

// EXAMPLE: new Office.Outlook()
function Outlook() {
    this.application = CreateObject("Outlook.Application");
    this.version = this.application.Version;

    console.info("Microsoft Office Outlook", this.version);

    this.namespace = this.application.GetNamespace("MAPI");
    this.currentFolder = null;
    this.items = null;

    this.open = function () {
        try {
            this.namespace.Logon("", "", false, false);
        } catch (e) {}
        this.selectFolder(Outlook.Folders.Inbox);
        return this;
    };

    this.close = function () {
        this.items = null;
        this.currentFolder = null;
        try { this.namespace.Logoff(); } catch (e) {}
        this.namespace = null;
        this.application = null;
    };

    this.selectFolder = function (folderIdOrPath) {
        if (typeof folderIdOrPath === "number") {
            this.currentFolder = this.namespace.GetDefaultFolder(folderIdOrPath);
        } else if (typeof folderIdOrPath === "string") {
            this.currentFolder = Outlook.resolveFolderPath(this.namespace, folderIdOrPath);
        } else {
            this.currentFolder = folderIdOrPath;
        }

        this.items = this.currentFolder.Items;
        this.items.Sort("[ReceivedTime]", true);
        return this;
    };

    this.getItems = function () {
        return new Outlook.Items(this.items);
    };

    this.find = function (filter) {
        var item = this.items.Find(filter);
        if (!item) return null;
        return new Outlook.MailItem(item);
    };

    this.restrict = function (filter) {
        var restricted = this.items.Restrict(filter);
        return new Outlook.Items(restricted);
    };

    this.createMail = function () {
        var mail = this.application.CreateItem(0);
        return new Outlook.MailItem(mail);
    };
}

Outlook.Folders = {
    Inbox: 6,
    Sent: 5,
    Outbox: 4,
    Drafts: 16,
    Deleted: 3,
    Junk: 23
};

Outlook.MailItemClass = 43;

Outlook.resolveFolderPath = function (mapiNamespace, path) {
    var parts = path.split("\\");
    var root = mapiNamespace.Folders.Item(1);
    var cur = root;

    for (var i = 0; i < parts.length; i++) {
        if (parts[i]) {
            cur = cur.Folders.Item(parts[i]);
        }
    }
    return cur;
};

Outlook.Items = function (items) {
    this.items = items;

    this.count = function () {
        return this.items.Count;
    };

    this.get = function (idx) {
        var it = this.items.Item(idx);
        if (it.Class === Outlook.MailItemClass) {
            return new Outlook.MailItem(it);
        }
        return new Outlook.Item(it);
    };

    this.forEach = function (fn, maxCount) {
        var n = this.count();
        if (typeof maxCount === "number" && maxCount > 0 && maxCount < n) {
            n = maxCount;
        }
        for (var i = 1; i <= n; i++) {
            fn(this.get(i), i);
        }
    };
};

Outlook.Item = function (item) {
    this.item = item;

    this.getClass = function () {
        return this.item.Class;
    };

    this.getSubject = function () {
        return this.item.Subject;
    };
};

Outlook.MailItem = function (mail) {
    this.mail = mail;

    this.getSubject = function () {
        return this.mail.Subject;
    };

    this.getSenderName = function () {
        return this.mail.SenderName;
    };

    this.getSenderEmailAddress = function () {
        return this.mail.SenderEmailAddress;
    };

    this.getReceivedTime = function () {
        return this.mail.ReceivedTime;
    };

    this.getBody = function () {
        return this.mail.Body;
    };

    this.getHtmlBody = function () {
        return this.mail.HTMLBody;
    };

    this.getUnread = function () {
        return this.mail.UnRead;
    };

    this.setUnread = function (value) {
        this.mail.UnRead = !!value;
        return this;
    };

    this.send = function () {
        this.mail.Send();
        return true;
    };

    this.save = function () {
        this.mail.Save();
        return true;
    };

    this.setTo = function (to) {
        this.mail.To = to;
        return this;
    };

    this.setCc = function (cc) {
        this.mail.CC = cc;
        return this;
    };

    this.setBcc = function (bcc) {
        this.mail.BCC = bcc;
        return this;
    };

    this.setSubject = function (subject) {
        this.mail.Subject = subject;
        return this;
    };

    this.setBody = function (body) {
        this.mail.Body = body;
        return this;
    };

    this.setHtmlBody = function (html) {
        this.mail.HTMLBody = html;
        return this;
    };
};

Outlook.FileExtensions = FileTypes.getExtensionsByOpenWith("msoutlook");

exports.Excel = Excel;
exports.PowerPoint = PowerPoint;
exports.Word = Word;
exports.Outlook = Outlook;

exports.VERSIONINFO = "Microsoft Office interface (msoffice.js) version 0.2.3";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
