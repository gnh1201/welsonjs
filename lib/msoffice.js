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
        return this;
    };

    this.addSlide = function(layout) {
        if (!this.currentPresentation) throw new Error("No presentation is open");
        if (typeof layout === "undefined") layout = PowerPoint.SlideLayouts.Blank;
        var index = this.currentPresentation.Slides.Count + 1;
        return new PowerPoint.Slide(this.currentPresentation.Slides.Add(index, layout));
    };

    this.duplicateSlide = function(idx, position) {
        if (!this.currentPresentation) throw new Error("No presentation is open");
        var source = this.currentPresentation.Slides.Item(idx);
        var duplicate = source.Duplicate();
        if (typeof position === "number") {
            if (position < 1 || position > this.currentPresentation.Slides.Count) {
                throw new Error("Slide position must be within the presentation slide range");
            }
            duplicate.MoveTo(position);
        }
        return new PowerPoint.Slide(duplicate);
    };

    this.selectSlide = function(idx) {
        if (!this.currentPresentation) throw new Error("No presentation is open");
        if (idx === 0) {
            var view = this.application.ActiveWindow.View;
            this.currentSlide = new PowerPoint.Slide(view.Slide);
        } else {
            this.currentSlide = new PowerPoint.Slide(this.currentPresentation.Slides.Item(idx));
        }
        this.currentSlide.slide.Select();
        return this.currentSlide;
    };

    this.getSlideCount = function() {
        if (!this.currentPresentation) return 0;
        return this.currentPresentation.Slides.Count;
    };

    this.exportAsPDF = function(filename) {
        if (!this.currentPresentation) throw new Error("No presentation is open");
        if (!filename) throw new Error("A PDF filename is required");
        try {
            this.currentPresentation.ExportAsFixedFormat(filename, PowerPoint.FixedFormatType.PDF);
            return FILE.fileExists(filename);
        } catch (e) {
            console.error("Could not export a PDF:", e.message);
            return false;
        }
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
            this.currentPresentation.SaveAs(filename);
            return FILE.fileExists(filename);
        } catch (e) {
            console.error("Could not save a file:", e.message);
            return false;
        }
    };

    this.close = function() {
        try {
            if (this.currentPresentation) this.currentPresentation.Close();
            this.currentPresentation = null;
            this.currentSlide = null;
            if (this.application) this.application.Quit();
            this.application = null;
        } catch (e) {
            this.currentPresentation = null;
            this.application = null;
        }
    };

    // get all slides
    this.getSlides = function() {
        var slides = [];
        if (!this.currentPresentation) return slides;

        var slideCount = this.currentPresentation.Slides.Count;
        for (var i = 1; i <= slideCount; i++) {
            slides.push(new PowerPoint.Slide(this.currentPresentation.Slides.Item(i)));
        }

        return slides;
    };

    this.getActiveSlide = function() {
        if (!this.application || !this.application.ActiveWindow) return null;
        var slide = this.application.ActiveWindow.View.Slide;
        return slide ? new PowerPoint.Slide(slide) : null;
    };
}
PowerPoint.SlideLayouts = {
    Title: 1,
    TitleAndContent: 2,
    SectionHeader: 3,
    TwoContent: 4,
    Comparison: 5,
    TitleOnly: 11,
    Blank: 12
};
PowerPoint.Slide = function(slide) {
    this.slide = slide;

    this.getShapes = function() {
        var shapes = [];
        var shapeCount = this.slide.Shapes.Count;
        for (var i = 1; i <= shapeCount; i++) {
            shapes.push(new PowerPoint.Shape(this.slide.Shapes.Item(i)));
        }
        return shapes;
    };

    this.getShapeCount = function() {
        return this.slide.Shapes.Count;
    };

    this.getShape = function(idx) {
        return new PowerPoint.Shape(this.slide.Shapes.Item(idx));
    };

    this.getIndex = function() {
        return this.slide.SlideIndex;
    };

    this.moveTo = function(position) {
        if (typeof position !== "number" || position < 1) throw new Error("Slide position must be a positive number");
        this.slide.MoveTo(position);
        return this;
    };

    this.isHidden = function() {
        return this.slide.SlideShowTransition.Hidden ? true : false;
    };

    this.setHidden = function(hidden) {
        this.slide.SlideShowTransition.Hidden = hidden ? -1 : 0;
        return this;
    };

    this.getTitle = function() {
        try { return this.slide.Shapes.Title.TextFrame.TextRange.Text; } catch (e) {
            try { return this.slide.Shapes.Item("WelsonJSTitle").TextFrame.TextRange.Text; } catch (e2) { return ""; }
        }
    };

    this.setTitle = function(text) {
        var title;
        try { title = this.slide.Shapes.Title; } catch (e) { title = null; }
        if (!title) {
            try { title = this.slide.Shapes.AddTitle(); } catch (e2) {
                title = this.slide.Shapes.AddTextbox(PowerPoint.Orientation.Horizontal, 20, 20, 600, 60);
                title.Name = "WelsonJSTitle";
            }
        }
        title.TextFrame.TextRange.Text = PowerPoint.decodeUnicodeEscapes(text);
        return new PowerPoint.Shape(title);
    };

    this.addPicture = function(filename, left, top, width, height) {
        if (!filename || !FILE.fileExists(filename)) throw new Error("Picture file was not found");
        return new PowerPoint.Shape(this.slide.Shapes.AddPicture(filename, 0, -1,
            PowerPoint._numberOr(left, 0), PowerPoint._numberOr(top, 0),
            PowerPoint._numberOr(width, -1), PowerPoint._numberOr(height, -1)));
    };

    // Office AutoShape IDs are exposed in PowerPoint.Shapes below.
    this.addShape = function(type, left, top, width, height) {
        if (typeof type !== "number") throw new Error("Shape type must be a PowerPoint shape constant");
        return new PowerPoint.Shape(this.slide.Shapes.AddShape(type,
            PowerPoint._numberOr(left, 0), PowerPoint._numberOr(top, 0),
            PowerPoint._numberOr(width, 100), PowerPoint._numberOr(height, 60)));
    };

    this.addRectangle = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Rectangle, left, top, width, height);
    };

    this.addRoundedRectangle = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.RoundedRectangle, left, top, width, height);
    };

    this.addEllipse = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Ellipse, left, top, width, height);
    };

    this.addTriangle = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Triangle, left, top, width, height);
    };

    this.addDiamond = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Diamond, left, top, width, height);
    };

    this.addParallelogram = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Parallelogram, left, top, width, height);
    };

    this.addTrapezoid = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Trapezoid, left, top, width, height);
    };

    this.addHexagon = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Hexagon, left, top, width, height);
    };

    this.addStar = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.Star5, left, top, width, height);
    };

    this.addArrow = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.RightArrow, left, top, width, height);
    };

    this.addLeftArrow = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.LeftArrow, left, top, width, height);
    };

    this.addUpArrow = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.UpArrow, left, top, width, height);
    };

    this.addDownArrow = function(left, top, width, height) {
        return this.addShape(PowerPoint.Shapes.DownArrow, left, top, width, height);
    };

    this.addLine = function(x1, y1, x2, y2) {
        return new PowerPoint.Shape(this.slide.Shapes.AddLine(x1, y1, x2, y2));
    };

    this.setBackgroundColor = function(rgb) {
        this.slide.Background.Fill.ForeColor.RGB = rgb;
        return this;
    };

    this.addText = function(text, left, top, width, height) {
        var shape = this.slide.Shapes.AddTextbox(PowerPoint.Orientation.Horizontal,
            PowerPoint._numberOr(left, 0), PowerPoint._numberOr(top, 0),
            PowerPoint._numberOr(width, 300), PowerPoint._numberOr(height, 60));
        shape.TextFrame.TextRange.Text = PowerPoint.decodeUnicodeEscapes(text);
        return new PowerPoint.Shape(shape);
    };

    this.remove = function() {
        this.slide.Delete();
        this.slide = null;
        return true;
    };
};
PowerPoint.Shape = function(shape) {
    this.shape = shape;

    this.getName = function() { return this.shape.Name; };
    this.getType = function() { return this.shape.Type; };
    this.getPosition = function() { return { left: this.shape.Left, top: this.shape.Top }; };
    this.getSize = function() { return { width: this.shape.Width, height: this.shape.Height }; };
    this.setName = function(name) { this.shape.Name = name; return this; };
    this.getText = function() {
        try { return this.shape.TextFrame.TextRange.Text; } catch (e) { return ""; }
    };
    this.setText = function(text) {
        this.shape.TextFrame.TextRange.Text = PowerPoint.decodeUnicodeEscapes(text);
        return this;
    };
    this.setPosition = function(left, top) {
        this.shape.Left = left;
        this.shape.Top = top;
        return this;
    };
    this.setSize = function(width, height) {
        this.shape.Width = width;
        this.shape.Height = height;
        return this;
    };
    this.setFillColor = function(rgb) {
        this.shape.Fill.ForeColor.RGB = rgb;
        return this;
    };
    this.setLineColor = function(rgb) {
        this.shape.Line.ForeColor.RGB = rgb;
        return this;
    };
    this.setFillTransparency = function(percent) {
        this.shape.Fill.Transparency = Math.max(0, Math.min(100, percent));
        return this;
    };
    this.setLineWeight = function(points) {
        this.shape.Line.Weight = points;
        return this;
    };
    this.setLineTransparency = function(percent) {
        this.shape.Line.Transparency = Math.max(0, Math.min(100, percent));
        return this;
    };
    this.setNoFill = function() {
        this.shape.Fill.Visible = 0;
        return this;
    };
    this.setNoLine = function() {
        this.shape.Line.Visible = 0;
        return this;
    };
    this.setRotation = function(degrees) {
        this.shape.Rotation = degrees;
        return this;
    };
    this.setTextStyle = function(options) {
        options = options || {};
        var range = this.shape.TextFrame.TextRange;
        if (typeof options.fontName !== "undefined") range.Font.Name = options.fontName;
        if (typeof options.fontSize !== "undefined") range.Font.Size = options.fontSize;
        if (typeof options.color !== "undefined") range.Font.Color.RGB = options.color;
        if (typeof options.bold !== "undefined") range.Font.Bold = options.bold ? -1 : 0;
        if (typeof options.italic !== "undefined") range.Font.Italic = options.italic ? -1 : 0;
        if (typeof options.alignment !== "undefined") range.ParagraphFormat.Alignment = options.alignment;
        if (typeof options.verticalAlignment !== "undefined") this.shape.TextFrame.VerticalAnchor = options.verticalAlignment;
        if (typeof options.margin !== "undefined") {
            this.shape.TextFrame.MarginLeft = options.margin;
            this.shape.TextFrame.MarginRight = options.margin;
            this.shape.TextFrame.MarginTop = options.margin;
            this.shape.TextFrame.MarginBottom = options.margin;
        }
        return this;
    };
    this.bringToFront = function() {
        this.shape.ZOrder(PowerPoint.ZOrder.BringToFront);
        return this;
    };
    this.sendToBack = function() {
        this.shape.ZOrder(PowerPoint.ZOrder.SendToBack);
        return this;
    };
    this.remove = function() {
        this.shape.Delete();
        this.shape = null;
        return true;
    };
};
PowerPoint.Orientation = { Horizontal: 1, Vertical: 2 };
PowerPoint.Shapes = {
    Rectangle: 1,
    Parallelogram: 2,
    Trapezoid: 3,
    Diamond: 4,
    RoundedRectangle: 5,
    Hexagon: 10,
    Triangle: 7,
    RightTriangle: 8,
    Ellipse: 9,
    Star5: 92,
    RightArrow: 33,
    LeftArrow: 34,
    UpArrow: 35,
    DownArrow: 36,
    Heart: 21
};
PowerPoint.ZOrder = { BringToFront: 0, SendToBack: 1 };
PowerPoint.FixedFormatType = { PDF: 2, XPS: 1 };
PowerPoint._numberOr = function(value, fallback) {
    return (typeof value === "number") ? value : fallback;
};
// Allows ASCII-only script sources (including evaluate_js) to pass Unicode text as \\uXXXX escapes.
PowerPoint.decodeUnicodeEscapes = function(value) {
    if (typeof value !== "string") return value;
    return value.replace(/\\u([0-9a-fA-F]{4})/g, function(match, hex) {
        return String.fromCharCode(parseInt(hex, 16));
    });
};
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
            console.info("Outlook MAPI session established");
        } catch (e) {
            console.warn("Outlook MAPI session already active or logon skipped");
        }

        this.selectFolder(Outlook.Folders.Inbox);
        console.info("Outlook folder selected: Inbox");

        return this;
    };

    this.close = function () {
        this.items = null;
        this.currentFolder = null;

        try {
            this.namespace.Logoff();
            console.info("Outlook MAPI session closed");
        } catch (e) {
            console.warn("Outlook MAPI session logoff skipped");
        }

        this.namespace = null;
        this.application = null;
        console.info("Outlook automation released");
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
        this.items.Sort("[ReceivedTime]", true); // newest first
        return this;
    };

    this.getItems = function () {
        return new Outlook.Items(this.items);
    };

    this.find = function (filter) {
        console.log(filter);
        var item = this.items.Find(filter);
        if (!item) return null;
        return new Outlook.MailItem(item);
    };

    this.restrict = function (filter) {
        console.log(filter);
        var restricted = this.items.Restrict(filter);
        return new Outlook.Items(restricted);
    };

    this.createMail = function () {
        var mail = this.application.CreateItem(0); // 0 = olMailItem
        return new Outlook.MailItem(mail);
    };

    // -----------------------------
    // Search helpers
    // -----------------------------

    this.searchBySenderContains = function (keyword) {
        // Jet-compatible sender filter (NOT SenderEmailAddress)
        return this.restrict(Outlook.Search.filters.senderContains_Jet(keyword));
    };

    this.searchByRecipientContains = function (keyword) {
        // DASL recipient filter
        return this.restrict(Outlook.Search.filters.recipientContains_DASL(keyword));
    };

    this.searchBySenderOrRecipientContains = function (keyword) {
        // IMPORTANT: cannot mix Jet and DASL in a single Restrict string.
        // Use DASL for sender + recipients and merge results.
        var bySender = this.restrict(Outlook.Search.filters.senderContains_DASL(keyword));
        var byRecipients = this.restrict(Outlook.Search.filters.recipientContains_DASL(keyword));

        var merged = new Outlook.ItemsMerged(bySender, byRecipients);

        return new Outlook.ItemsFiltered(merged, function (mailItem) {
            return Outlook.Search.match.senderOrRecipientObjectContains(mailItem, keyword);
        });
    };

    this.searchBySenderEmailEquals = function (email) {
        // Best-effort (display-based) DASL match
        return this.restrict(Outlook.Search.filters.senderEmailEquals(email));
    };

    this.searchUnread = function () {
        return this.restrict("[Unread] = True");
    };

    this.searchSince = function (dateObj) {
        return this.restrict(Outlook.Search.filters.receivedSince(dateObj));
    };

    this.searchSubjectContains = function (keyword) {
        return this.restrict(Outlook.Search.filters.subjectContains(keyword));
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
    // path examples:
    // - "Inbox\\SubFolder"
    // - "Mailbox - Name\\Inbox\\SubFolder" (store root name)
    var parts = path.split("\\");
    var cur = null;

    // If first segment matches a store root, start there; else start at default store root.
    var stores = mapiNamespace.Folders;
    for (var i = 1; i <= stores.Count; i++) {
        var f = stores.Item(i);
        if ((f.Name + "") === (parts[0] + "")) {
            cur = f;
            parts.shift();
            break;
        }
    }
    if (!cur) cur = stores.Item(1);

    for (var j = 0; j < parts.length; j++) {
        if (parts[j]) cur = cur.Folders.Item(parts[j]);
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
        if (!it) return null;
        if (it.Class === Outlook.MailItemClass) return new Outlook.MailItem(it);
        return new Outlook.Item(it);
    };

    // callback(it, i) returns true => stop
    this.forEach = function (fn, maxCount) {
        var n = this.count();
        if (typeof maxCount === "number" && maxCount > 0 && maxCount < n) n = maxCount;

        for (var i = 1; i <= n; i++) {
            var stop = fn(this.get(i), i);
            if (stop === true) break;
        }
    };
};

Outlook.ItemsMerged = function (a, b) {
    // a,b can be Outlook.Items or COM Items
    this.a = (a instanceof Outlook.Items) ? a : new Outlook.Items(a);
    this.b = (b instanceof Outlook.Items) ? b : new Outlook.Items(b);

    this.count = function () {
        return this.a.count() + this.b.count();
    };

    this.get = function (idx) {
        return null; // not supported
    };

    // callback(it, i) returns true => stop
    this.forEach = function (fn, maxCount) {
        var seen = {};
        var emitted = 0;

        var emit = function (it, idx) {
            if (!it) return false;
            if (!(it instanceof Outlook.MailItem)) return false;

            var entryId = it.mail.EntryID;
            if (!entryId) entryId = String(it.getSubject()) + "|" + String(it.getReceivedTime());

            if (seen[entryId]) return false;
            seen[entryId] = true;

            var stop = fn(it, idx);
            emitted++;

            if (stop === true) return true;
            if (typeof maxCount === "number" && maxCount > 0 && emitted >= maxCount) return true;
            return false;
        };

        var na = this.a.count();
        for (var i = 1; i <= na; i++) {
            if (emit(this.a.get(i), i)) return;
        }

        var nb = this.b.count();
        for (var j = 1; j <= nb; j++) {
            if (emit(this.b.get(j), j)) return;
        }
    };
};

Outlook.ItemsFiltered = function (items, predicate) {
    // items: Outlook.Items or Outlook.ItemsMerged
    this.base = items;
    this.predicate = predicate;

    this.count = function () {
        if (this.base && this.base.count) return this.base.count();
        return 0;
    };

    this.get = function (idx) {
        return null; // not supported in generic filtered wrapper
    };

    // callback(it, i) returns true => stop
    this.forEach = function (fn, maxCount) {
        var emitted = 0;
        var self = this;

        this.base.forEach(function (it, i) {
            if (!it) return false;
            if (!(it instanceof Outlook.MailItem)) return false;

            if (self.predicate(it)) {
                var stop = fn(it, i);
                emitted++;

                if (stop === true) return true;
                if (typeof maxCount === "number" && maxCount > 0 && emitted >= maxCount) return true;
            }
            return false;
        });
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

    this.getClass = function () {
        return this.mail.Class;
    };

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

Outlook.Search = {};
Outlook.Search.filters = {};

// Jet escape
Outlook.Search.filters._escape = function (s) {
    return (s + "").replace(/'/g, "''");
};

// DASL escape (same)
Outlook.Search.filters._escapeDASL = function (s) {
    return (s + "").replace(/'/g, "''");
};

// Jet: Subject contains (wildcard = *)
Outlook.Search.filters.subjectContains = function (keyword) {
    var k = Outlook.Search.filters._escape(keyword);
    return "([Subject] Like '*" + k + "*')";
};

// Jet: Sender contains (use [From]/[SenderName], NOT SenderEmailAddress)
Outlook.Search.filters.senderContains = function (keyword) {
    var k = Outlook.Search.filters._escape(keyword);
    return "([From] Like '*" + k + "*') OR ([SenderName] Like '*" + k + "*')";
};

// DASL: From contains (wildcard = %)
Outlook.Search.filters.senderContains_DASL = function (keyword) {
    var k = Outlook.Search.filters._escapeDASL(keyword);
    return '@SQL="urn:schemas:httpmail:from" LIKE \'%' + k + '%\'';
};

// DASL: Recipient contains (To/CC/BCC display strings)
Outlook.Search.filters.recipientContains = function (keyword) {
    var k = Outlook.Search.filters._escapeDASL(keyword);
    return '@SQL=' +
        '"urn:schemas:httpmail:displayto" LIKE \'%' + k + '%\' OR ' +
        '"urn:schemas:httpmail:displaycc" LIKE \'%' + k + '%\' OR ' +
        '"urn:schemas:httpmail:displaybcc" LIKE \'%' + k + '%\'';
};

// Compatibility aliases
Outlook.Search.filters.senderContains_Jet = function (keyword) {
    return Outlook.Search.filters.senderContains(keyword);
};

Outlook.Search.filters.recipientContains_DASL = function (keyword) {
    return Outlook.Search.filters.recipientContains(keyword);
};

// Best-effort: sender "equals" (display-from match)
Outlook.Search.filters.senderEmailEquals = function (email) {
    var e = Outlook.Search.filters._escapeDASL(email);
    return '@SQL="urn:schemas:httpmail:from" LIKE \'%' + e + '%\'';
};

// Jet: received since (locale-dependent date string)
Outlook.Search.filters.receivedSince = function (dateObj) {
    return "([ReceivedTime] >= '" + dateObj + "')";
};

// DASL: received since
Outlook.Search.filters.receivedSince_DASL = function (dateObj) {
    var d = Outlook.Search.filters._escapeDASL(dateObj);
    return '@SQL="DAV:date-received" >= \'' + d + '\'';
};

Outlook.Search.match = {};

Outlook.Search.match._contains = function (hay, needle) {
    return (hay + "").toLowerCase().indexOf((needle + "").toLowerCase()) >= 0;
};

Outlook.Search.match.senderOrRecipientObjectContains = function (mailItem, keyword) {
    var k = keyword + "";

    if (Outlook.Search.match._contains(mailItem.getSenderEmailAddress() || "", k)) return true;
    if (Outlook.Search.match._contains(mailItem.getSenderName() || "", k)) return true;

    if (Outlook.Search.match._contains(mailItem.mail.To || "", k)) return true;
    if (Outlook.Search.match._contains(mailItem.mail.CC || "", k)) return true;
    if (Outlook.Search.match._contains(mailItem.mail.BCC || "", k)) return true;

    var r = mailItem.mail.Recipients;
    for (var i = 1; i <= r.Count; i++) {
        var ri = r.Item(i);
        if (Outlook.Search.match._contains(ri.Address || "", k)) return true;
        if (Outlook.Search.match._contains(ri.Name || "", k)) return true;
    }

    return false;
};

Outlook.FileExtensions = FileTypes.getExtensionsByOpenWith("msoutlook");

/*
EXAMPLE:

var ol = new Outlook().open().selectFolder(Outlook.Folders.Inbox);

// sender contains
ol.searchBySenderContains("amazon.com").forEach(function(m) {
    console.log("[S] " + m.getSenderEmailAddress() + " | " + m.getSubject());
}, 10);

// recipient contains
ol.searchByRecipientContains("me@example.com").forEach(function(m) {
    console.log("[R] " + (m.mail.To || "") + " | " + m.getSubject());
}, 10);

// sender OR recipient contains (includes Recipients collection check)
ol.searchBySenderOrRecipientContains("team@company.com").forEach(function(m) {
    console.log("[SR] " + m.getSubject());
}, 10);

ol.close();
*/

exports.Excel = Excel;
exports.PowerPoint = PowerPoint;
exports.Word = Word;
exports.Outlook = Outlook;

exports.VERSIONINFO = "Microsoft Office interface (msoffice.js) version 0.2.3";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
