// hwp.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
//
// Hancom Hangul (HWP) automation interface for WelsonJS.

var FILE = require("lib/file");
var SHELL = require("lib/shell");
var SYS = require("lib/system");

var DEFAULT_EXE_PATH = "C:\\Program Files (x86)\\HNC\\Office 2020\\HOffice110\\Bin\\Hwp.exe";
var DEFAULT_OPEN_ARGUMENTS = "forceopen:True;suspendpassword:True;versionwarning:False";

function HwpObject(options) {
    options = options || {};
    this.exePath = options.exePath || DEFAULT_EXE_PATH;
    this.visible = (typeof options.visible === "boolean") ? options.visible : true;
    this.application = null;

    this.setExePath = function(path) {
        if (typeof path !== "string" || path.length === 0) throw new Error("HWP executable path is required");
        this.exePath = path;
        return this;
    };

    this.setVisible = function(flag) {
        this.visible = !!flag;
        if (this.application) {
            try { this.application.XHwpWindows.Item(0).Visible = this.visible; } catch (e) {}
        }
        return this;
    };

    this.launch = function() {
        if (!FILE.fileExists(this.exePath)) throw new Error("HWP executable does not exist: " + this.exePath);
        var shell = SHELL.create().setVisibility(this.visible ? "visible" : "hidden");
        shell.run('"' + this.exePath + '"', true);
        shell.release();
        sleep(5000);
        return this;
    };

    this.getInstance = function() {
        if (this.application) return this.application;
        this.application = CreateObject("HWPFrame.HwpObject");
        if (!this.application) {
            this.launch();
            this.application = CreateObject("HWPFrame.HwpObject");
        }
        if (!this.application) throw new Error("Could not create HWP COM instance");
        this.setVisible(this.visible);
        return this.application;
    };

    this.registerModule = function(moduleType, moduleName) {
        return this.getInstance().RegisterModule(moduleType || "FilePathCheckDLL", moduleName || "FilePathCheckerModule");
    };

    this.open = function(filename, format, openArguments) {
        if (typeof filename !== "string" || filename.length === 0) throw new Error("HWP filename is required");
        if (!FILE.isAbsolutePath(filename)) filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;
        if (!FILE.fileExists(filename)) throw new Error("HWP file does not exist: " + filename);
        this.getInstance().Open(filename, format || "HWP", typeof openArguments === "string" ? openArguments : DEFAULT_OPEN_ARGUMENTS);
        return this;
    };

    this.insertText = function(text) {
        if (typeof text !== "string") text = String(text);
        var hwp = this.getInstance();
        var set = hwp.HParameterSet.HInsertText;
        hwp.HAction.GetDefault("InsertText", set.HSet);
        set.Text = text;
        return hwp.HAction.Execute("InsertText", set.HSet);
    };

    this.replaceText = function(source, target) {
        if (typeof source !== "string" || typeof target !== "string") throw new Error("Source and target must be strings");
        var hwp = this.getInstance();
        var set = hwp.HParameterSet.HFindReplace;
        hwp.HAction.GetDefault("AllReplace", set.HSet);
        set.FindString = source;
        set.ReplaceString = target;
        set.Direction = 2;
        set.ReplaceMode = 1;
        set.IgnoreMessage = 1;
        set.IgnoreFindString = 0;
        set.IgnoreReplaceString = 0;
        set.FindType = 1;
        return hwp.HAction.Execute("AllReplace", set.HSet);
    };

    this.insertPicture = function(filename, options) {
        if (typeof filename !== "string" || filename.length === 0) throw new Error("Image filename is required");
        if (!FILE.isAbsolutePath(filename)) filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;
        if (!FILE.fileExists(filename)) throw new Error("Image file does not exist: " + filename);
        options = options || {};
        var hwp = this.getInstance();
        var embedded = (typeof options.embedded === "boolean") ? options.embedded : true;
        var sizeOption = (typeof options.sizeOption === "number") ? options.sizeOption : 0;
        var reverse = !!options.reverse;
        var watermark = !!options.watermark;
        var effect = (typeof options.effect === "number") ? options.effect : 0;
        if (typeof options.width === "number" && typeof options.height === "number") {
            return hwp.InsertPicture(filename, embedded, sizeOption, reverse, watermark, effect, options.width, options.height);
        }
        return hwp.InsertPicture(filename, embedded, sizeOption, reverse, watermark, effect);
    };

    this.saveAs = function(filename, format, saveArguments) {
        if (typeof filename !== "string" || filename.length === 0) throw new Error("Destination filename is required");
        if (!FILE.isAbsolutePath(filename)) filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;
        return this.getInstance().SaveAs(filename, format || "HWP", saveArguments || "");
    };

    this.saveAsPdf = function(filename) {
        if (typeof filename !== "string" || filename.length === 0) throw new Error("PDF filename is required");
        if (!FILE.isAbsolutePath(filename)) filename = SYS.getCurrentWorkingDirectory() + "\\" + filename;
        var hwp = this.getInstance();
        var set = hwp.HParameterSet.HFileOpenSave;
        hwp.HAction.GetDefault("FileSaveAs_S", set.HSet);
        set.filename = filename;
        set.Format = "PDF";
        return hwp.HAction.Execute("FileSaveAs_S", set.HSet);
    };

    this.quit = function() {
        if (!this.application) return true;
        try { this.application.Quit(); } finally { this.application = null; }
        return true;
    };
    this.close = this.quit;
}

exports.create = function(options) { return new HwpObject(options); };
exports.start = function(options) { var hwp = new HwpObject(options); hwp.getInstance(); return hwp; };
exports.VERSIONINFO = "Hancom HWP Automation Interface (hwp.js) version 0.1.0";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = require;
