// gtk.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// GTKServer API - GTK GUI Programming with WSH (Windows Scripting Host)
// 
var SHELL = require("lib/shell");

// set binPath
var binPath = "bin\\gtk-server";

// start GTKServer
var GTKServer = SHELL.createProcess([binPath, "-stdin"]);

// Common (Widgets)
var GTKWidgets = {};

// Common (Element)
var GTKWidget = function(options) {
    this.widgetType = "GTKWidget";
    this.widgetID = "0";

    this.dispatchEvent = function(event) {
        var eventName = 'on' + event.eventName;

        event.target = this;
        if(eventName in this) {
            this[eventName](event);
        }
    };
    this.addEventListener = function(eventName, fn) {
        if (typeof(fn) == "function") {
            this['on' + eventName] = fn;
        } else {
            throw new TypeError("EventListener must be a function");
        }
    };
    this.eventListener = function() {
        this.dispatchEvent(new GTKEvent("wait"));
        return GTKEventListener(this);
    };
    this.setWidgetType = function(widgetType) {
        this.widgetType = widgetType;
    };
    this.update = function() {
        if (typeof(options) === "object") {
            for (var k in options) {
                this[k] = options[k];
            }
        }
    };
    this.create = function() {
        this.update();
        this.dispatchEvent(new GTKEvent("load"));
        this.widgetID = GTKCreateWidget(this);
        GTKWidgets[this.widgetID] = this;
    }
};

// Common (definedEvents)
var definedEvents = {
    "Window": [],
    "Table": [],
    "Button": ["click"],
    "Entry": ["enter"],
    "RadioBox": ["click"],
    "TextBox": []
};

// Common (Event)
var GTKEvent = function(eventName) {
    this.eventName = eventName;
    this.target = null;
};

// GTKEventListener
var GTKEventListener = function(widget) {
    if (widget.widgetType in definedEvents) {
        var widgetEvents = definedEvents[widget.widgetType];
        for (var i = 0; i < widgetEvents.length; i++) {
            widget.dispatchEvent(new GTKEvent(widgetEvents[i]));
        }
    }
};

// GTKCreateWidget
var GTKCreateWidget = function(widget) {
    var widgetID, commands = [];

    switch (widget.widgetType) {
        case "Window":
            commands.push([
                "gtk_window_new",
                widget.type
            ]);
            break;

        case "Table":
            commands.push([
                "gtk_table_new",
                widget.rows,
                widget.columns,
                widget.homogeneous
            ]);
            break;

        case "Button":
            commands.push([
                "gtk_button_new_with_label",
                "\"" + widget.text + "\""
            ]);
            break;

        case "Entry":
            commands.push([
                "gtk_entry_new",
                "NULL",
                "NULL"
            ]);

            break;

        case "RadioBox":
            commands.push([
                "gtk_radio_button_new_with_label_from_widget",
                widget.memberWidget.widgetID,
                "\"" + widget.text + "\""
            ]);
            break;

        case "TextBox":
            commands.push([
                "gtk_text_new",
                "NULL",
                "NULL"
            ]);
            break;
    }

    while (commands.length > 0) {
        widgetID = GTKExecCommand(commands.pop());
    }

    return widgetID;
};

// GTKExecCommand
var GTKExecCommand = function(command) {
    var line, _command = [];

    for (var i = 0; i < command.length; i++) {
        var term = command[i];

        if (typeof(term) == "number") {
            _command.push(term == 0 ? '0' : term);
        } else if (typeof(term) == "boolean") {
            _command.push(!term ? '0' : '1');
        } else if (typeof(term) == "string") {
            _command.push(term);
        }
    }

    line = _command.join(' ');
    
    console.log(line);

    GTKServer.StdIn.WriteLine(line);
    return GTKServer.StdOut.ReadLine();
};

// GTKInit
var GTKInit = function(callback) {
    GTKExecCommand([
        "gtk_init",
        "NULL",
        "NULL"
    ]);

    if (typeof(callback) == "function") {
        callback();
    }
};

// GTKExit
var GTKExit = function() {
    return GTKExecCommand([
        "gtk_server_exit"
    ]);
};

// Window
var Window = function() {
    GTKWidget.apply(this, arguments);

    this.widgetType = "Window";
    this.type = 0;
    this.title = "WelsonJS GTK GUI Application";
    this.width = 450;
    this.height = 400;
    this.containerWidget = null; 

    this.setContainer = function(widget) {
        this.containerWidget = widget;
        return GTKExecCommand([
            "gtk_container_add",
            this.widgetID,
            widget.widgetID
        ]);
    };
    this.show = function() {
        return GTKExecCommand([
            "gtk_widget_show_all",
            this.widgetID
        ]);
    };
    this.setWidgetType(this.widgetType);
    this.create();

    GTKExecCommand([
        "gtk_window_set_title",
        this.widgetID,
        "\"" + this.title + "\""
    ]);

    GTKExecCommand([
        "gtk_widget_set_usize",
        this.widgetID,
        this.width,
        this.height
    ]);
};
Window.prototype = new GTKWidget();
Window.prototype.constructor = Window;

// Table
var Table = function() {
    GTKWidget.apply(this, arguments);

    this.widgetType = "Table";
    this.rows = 1;
    this.columns = 1;
    this.homogeneous = true;
    this.attachedWidgets = [];

    this.attach = function(widget, left, right, top, bottom) {
        this.attachedWidgets.push(widget);

        return GTKExecCommand([
            "gtk_table_attach_defaults",
            this.widgetID,
            widget.widgetID,
            left,
            right,
            top,
            bottom
        ]);
    };
    
    this.setWidgetType(this.widgetType);
    this.create();
};
Table.prototype = new GTKWidget();
Table.prototype.constructor = Table;

// Button
var Button = function() {
    GTKWidget.apply(this, arguments);

    this.widgetType = "Button";
    this.text = "New Button";

    this.setWidgetType(this.widgetType);
    this.create();
};
Button.prototype = new GTKWidget();
Button.prototype.constructor = Button;

// Entry
var Entry = function() {
    GTKWidget.apply(this, arguments);

    this.widgetType = "Entry";
    this.text = "New Label";

    this.focus = function() {
        return GTKExecCommand([
            "gtk_widget_grab_focus",
            this.widgetID
        ]);
    };

    this.empty = function() {
        GTKExecCommand([
            "gtk_editable_delete_text",
            this.widgetID,
            0,
            -1
        ]);
    };
    this.getText = function() {
        this.text = GTKExecCommand([
            "gtk_entry_get_text",
            this.widgetID
        ]);

        return this.text;
    };
    this.setText = function(text) {
        this.text = text;

        GTKExecCommand([
            "gtk_entry_set_text",
            this.widgetID,
            "\"" + this.text + "\""
        ]);
    };

    this.setText(this.text);
    this.setWidgetType(this.widgetType);
    this.create();
};
Entry.prototype = new GTKWidget();
Entry.prototype.constructor = Entry;

// RadioBox
var RadioBox = function() {
    GTKWidget.apply(this, arguments);

    this.widgetType = "RadioBox";
    this.text = "New RadioBox";
    this.group = {
        widgetID: "NULL"
    };
    this.memberWidget = {
        widgetID: "NULL"
    };

    this.setMemberWidget = function(widget) {
        this.memberWidget = widget;
    };

    this.setWidgetType(this.widgetType);

    this.update();
    if (this.group.widgetID != "NULL") {
        this.group.add(this);
    }

    this.create();
};
RadioBox.prototype = new GTKWidget();
RadioBox.prototype.constructor = RadioBox;

// RadioGroup
var RadioGroup = function() {
    GTKWidget.apply(this, arguments);

    this.memberWidgets = [];
    this.widgetType = "RadioGroup";

    this.add = function(widget) {
        if (this.memberWidgets.length > 0) {
            widget.setMemberWidget(this.memberWidgets[0]);
        }
        this.memberWidgets.push(widget);
    };
};
RadioBox.prototype = new GTKWidget();
RadioBox.prototype.constructor = RadioGroup;

// TextBox
var TextBox = function() {
    GTKWidget.apply(this, arguments);

    this.widgetType = "TextBox";
    this.text = "New TextBox";

    this.setText = function(text) {
        this.text = text;
        GTKExecCommand([
            "gtk_text_insert",
            this.widgetID,
            "NULL",
            "NULL",
            "NULL",
            "\"" + this.text + "\"",
            "-1"
        ]);
    };

    this.setText(this.text);
    this.setWidgetType(this.widgetType);
    this.create();
};
TextBox.prototype = new GTKWidget();
TextBox.prototype.constructor = TextBox;

// GTKWait
var GTKWait = function(callback) {
    var even;

    while (true) {
        even = GTKExecCommand([
            "gtk_server_callback",
            "wait"
        ]);

        if (even in GTKWidgets) {
            GTKWidgets[even].eventListener();
        }

        if (typeof(callback) == "function") {
            callback(even);
        }
    }

    GTKExit();
};

// GladeXML
var GladeXML = function() {
    var xml;

    this.load = function(filename) {
        xml = GTKExecCommand([
            "glade_xml_new",
            filename,
            "NULL",
            "NULL"
        ]);
    };

    this.findWidget = function(widgetName) {
        var widgetId = GTKExecCommand([
            "glade_xml_get_widget",
            xml,
            widgetName
        ]);
        var widget = new GTKWidget({
            widgetId: widgetId
        });
        widget.create();
        return widget;
    };
};

exports.Widget = GTKWidget;
exports.Window = Window;
exports.Table = Table;
exports.Button = Button;
exports.Entry = Entry;
exports.RadioBox = RadioBox;
exports.RadioGroup = RadioGroup;
exports.TextBox = TextBox;
exports.GladeXML = GladeXML;

exports.init = GTKInit;
exports.wait = GTKWait;
exports.exit = GTKExit;

exports.VERSIONINFO = "GTKServer Module (gtk.js) version 0.2";
exports.global = global;
exports.require = require;
