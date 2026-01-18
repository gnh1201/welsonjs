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
