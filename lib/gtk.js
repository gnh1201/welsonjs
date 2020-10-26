////////////////////////////////////////////////////////////////////////
// GTKServer API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

// set binPath
var binPath = "bin\\gtk-server.exe";

// Common (Elements)
var GTKElements = [];

// Common (Element)
var GTKElement = function() {
    this.type = "GTKElement";
    this.width = 0;
    this.height = 0;
    
    this.setWidth = function(width) {
        this.width = width;
    };
    this.setHeight = function(height) {
        this.height = height;
    };

    this,onchange = function() {};
    this.onclick = function() {};
    this.onmouseover = function() {};
    this.onmouseout = function() {};
    this.onkeydown = function() {};
    this.onload	= function() {};
    this.addEventListener = function(ev, fn) {
        if (typeof(fn) == "function") {
            this['on' + ev] = fn;
        } else {
            throw new TypeError("listener must be a function");
        }
    };

    GTKElements.push(this);
};

// start GTKServer
var = function() {
    SHELL.run([
        binPath,
        "-stdin"
    ]);
};

// Window
var Window = function() {
    GTKElement.apply(this, arguments);
    
    this.type = "Window";
    this.title = "WelsonJS GTK GUI Application";
    this.setTitle = function(title) {
        this.Title = title;
    };
};
Window.prototype = new GTKElement();
Window.prototype.constructor = Window;

// Table
var Table = function() {
    GTKElement.apply(this, arguments);
    
    this.type = "Table";
    this.attach = function(element, left, right, top, buttom) {
        // TODO: Table.attach()
    };
};
Table.prototype = new GTKElement();
Table.prototype.constructor = Table;

// Button
var Button = function() {
    GTKElement.apply(this, arguments);
    
    this.type = "Button";
    this.text = "New Button";
    this.setText = function(text) {
        this.Text = text;
    };
};
Button.prototype = new GTKElement();
Button.prototype.constructor = Button;

// Label
var Label = function() {
    GTKElement.apply(this, arguments);
    
    this.type = "Label";
    this.text = "New Label";
    this.setText = function(text) {
        this.text = text;
    };
};
Label.prototype = new GTKElement();
Label.prototype.constructor = Label;

// RadioBox
var RadioBox = function() {
    GTKElement.apply(this, arguments);
    
    this.type = "RadioBox";
    this.text = "New RadioBox";
    this.setText = function(text) {
        this.Text = text;
    };
};
RadioBox.prototype = new GTKElement();
RadioBox.prototype.constructor = RadioBox;

// CheckBox
var CheckBox = function() {
    GTKElement.apply(this, arguments);
    
    this.type = "CheckBox";
    this.text = "New CheckBox";
    this.setText = function(text) {
        this.Text = text;
    };
};
CheckBox.prototype = new GTKElement();
CheckBox.prototype.constructor = CheckBox;

// TextBox
var TextBox = function() {
    GTKElement.apply(this, arguments);
    
    this.type = "TextBox";
    this.text = "New TextBox";
    this.setText = function(text) {
        this.Text = text;
    };
};
TextBox.prototype = new GTKElement();
TextBox.prototype.constructor = TextBox;

exports.start = function(callback) {
    // TODO: start
};
