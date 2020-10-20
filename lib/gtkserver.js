////////////////////////////////////////////////////////////////////////
// GTKServer API
////////////////////////////////////////////////////////////////////////

// set binPath
var binPath = "bin\\gtk-server.exe";

// Common (Elements)
var GTKElements = [];

// Common (Element)
var GTKElement = function(elementType) {
    this.Width = 0;
    this.Height = 0;

    this.setWidth = function(width) {
        this.Width = width;
    };
    this.setHeight = function(height) {
        this.Height = height;
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

// Window
var Window = function() {
    GTKElement.apply(this, arguments);
    
    this.Type = "Window";
    this.Title = "WelsonJS GTK GUI Application";
    this.setTitle = function(title) {
        this.Title = title;
    };
};
Window.prototype = new GTKElement();
Window.prototype.constructor = Window;

// Table
var Table = function() {
    GTKElement.apply(this, arguments);
    
    this.Type = "Table";
    this.attach = function(element, left, right, top, buttom) {
        // TODO: Table.Attach()
    };
};
Table.prototype = new GTKElement();
Table.prototype.constructor = Table;

// Button
var Button = function() {
    GTKElement.apply(this, arguments);
    
    this.Type = "Button";
    this.Text = "New Button";
    this.setText = function(text) {
        this.Text = text;
    };
    
};
Button.prototype = new GTKElement();
Button.prototype.constructor = Button;

// Label
var Label = function() {
    GTKElement.apply(this, arguments);
    
    this.Type = "Label";
    this.Text = "New Label";
    this.setText = function(text) {
        this.Text = text;
    };
};
Label.prototype = new GTKElement();
Label.prototype.constructor = Label;

// RadioBox
var RadioBox = function() {
    GTKElement.apply(this, arguments);
    
    this.Type = "RadioBox";
    this.Text = "New RadioBox";
    this.setText = function(text) {
        this.Text = text;
    };
};
RadioBox.prototype = new GTKElement();
RadioBox.prototype.constructor = RadioBox;

// CheckBox
var CheckBox = function() {
    GTKElement.apply(this, arguments);
    
    this.Type = "CheckBox";
    this.Text = "New CheckBox";
    this.setText = function(text) {
        this.Text = text;
    };
};
CheckBox.prototype = new GTKElement();
CheckBox.prototype.constructor = CheckBox;

// TextBox
var TextBox = function() {
    GTKElement.apply(this, arguments);
    
    this.Type = "TextBox";
    this.Text = "New TextBox";
    this.setText = function(text) {
        this.Text = text;
    };
};
TextBox.prototype = new GTKElement();
TextBox.prototype.constructor = TextBox;

exports.start = function(callback) {
    // TODO: start
};
