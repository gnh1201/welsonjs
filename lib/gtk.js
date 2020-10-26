////////////////////////////////////////////////////////////////////////
// GTKServer API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

// set binPath
var binPath = "bin\\gtk-server.exe";

// start GTKServer
var GTKServer = SHELL.createExecObject([binPath, "-stdin"]);

// Common (Elements)
var GTKWidgets = {};

// Common (Element)
var GTKWidget = function(callback) {
    this.widgetType = "GTKWidget";
    this.widgetID = GTKCreateWidget(this);
    this.callback = callback;

    GTKWidgets[this.widgetID] = this;
};

// GTKCreateElement
var GTKCreateWidget = function(widget) {
    var command = [];

    switch(widget.widgetType) {
        case "Window":
            command.push("gtk_window_new");
            command.push(this.type);
            break;

        case "Table":
            command.push("gtk_table_new");
            command.push(this.rows);
            command.push(this.columns);
            command.push(this.homogeneous);

            // attach sub widgets to table
            var subWidgets = widget.attachedWidgets;
            for(var i = 0; i < subWidgets.length; i++) {
                GTKExecCommand([
                    "gtk_table_attach_defaults",
                    this.widgetID,
                    subWidgets[i].widget.widgetID,
                    subWidgets[i].left,
                    subWidgets[i].right,
                    subWidgets[i].top,
                    subWidgets[i].buttom
                ]);
            }
            break;

        case "Button":
            command.push("gtk_button_new_with_label");
            command,push(this.title);
            break;
            
        case "Entry":
            command.push("gtk_entry_new");
            break;

        case "RadioBox":
            command = "";
            break;
        
        case "CheckBox":
            command = "";
            break;

        case "TextBox":
            command.push("gtk_text_new");
            command.push("NULL");
            command.push("NULL");
            break;
    }

    return GTKExecCommand(command);
};

// GTKExecCommand
var GTKExecCommand = function(command) {
    var line, _command = [];

    for(var i = 0; i < command.length; i++) {
        if(typeof(command[i]) == "number") {
            _command.push( (command[i] < 1 ? '0' : command[i]) );
        } else if(typeof(command[i]) == "string") {
            _command.push(command[i]);
        }
    }

    line = _command.join(' ');

    GTKServer.StdIn.WriteLine(line);
    return GTKServer.StdOut.ReadLine();
};

// GTKInit
var GTKInit = function() {
    return GTKExecCommand([
        "gtk_init",
        "NULL"
        "NULL"
    ]);
};

// Window
var Window = function() {
    GTKWidget.apply(this, arguments);

    this.elementType = "Window";
    this.type = 0;
    this.title = "WelsonJS GTK GUI Application";

    this.show() = function() {
        return GTKExecCommand([
            "gtk_widget_show_all",
            this.widgetID
        ]);
    };
};
Window.prototype = new GTKWidget();
Window.prototype.constructor = Window;

// Table
var Table = function() {
    GTKWidget.apply(this, arguments);

    this.elementType = "Table";
    this.rows = 1;
    this.columns = 1;
    this.homogeneous = true;

    this.attachedWidgets = [];

    this.attach = function(widget, left, right, top, bottom) {
        this.attachedWidgets.push({
            "widget": widget,
            "left": left,
            "right": right,
            "top": top,
            "bottom": bottom
        });
    };
};
Table.prototype = new GTKWidget();
Table.prototype.constructor = Table;

// Button
var Button = function() {
    GTKWidget.apply(this, arguments);
    
    this.elementType = "Button";
    this.text = "New Button";
};
Button.prototype = new GTKWidget();
Button.prototype.constructor = Button;

// Entry
var Entry = function() {
    GTKWidget.apply(this, arguments);

    this.elementType = "Entry";
    this.text = "New Label";

    this.focus = function() {
        return GTKExecCommand([
            "gtk_widget_grab_focus",
            this.widgetID
        ]);
    };
};
Entry.prototype = new GTKWidget();
Entry.prototype.constructor = Entry;

// RadioBox
var RadioBox = function() {
    GTKWidget.apply(this, arguments);

    this.elementType = "RadioBox";
    this.text = "New RadioBox";
};
RadioBox.prototype = new GTKWidget();
RadioBox.prototype.constructor = RadioBox;

// CheckBox
var CheckBox = function() {
    GTKWidget.apply(this, arguments);

    this.elementType = "CheckBox";
    this.text = "New CheckBox";
};
CheckBox.prototype = new GTKWidget();
CheckBox.prototype.constructor = CheckBox;

// TextBox
var TextBox = function() {
    GTKWidget.apply(this, arguments);
    
    this.elementType = "TextBox";
    this.text = "New TextBox";
};
TextBox.prototype = new GTKWidget();
TextBox.prototype.constructor = TextBox;

// GTKWait
var GTKWait = function() {
    var even;

    while(true) {
        even = GTKExecCommand([
            "gtk_server_callback",
            "wait"
        ]);

        if(even in GTKWidgets) {
            GTKWidgets[even].callback();
        }
    }
};

exports.wait = GTKWait;
