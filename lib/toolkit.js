////////////////////////////////////////////////////////////////////////
// WelsonJS.Toolkit API
////////////////////////////////////////////////////////////////////////

var ToolkitObject = function() {
    this.interface = null;

    this.create = function() {
        try {
            this.interface = CreateObject("WelsonJS.Toolkit");
            return this;
        } catch (e) {
            console.warn("WelsonJS.Toolkit is disabled");
            console.warn(e.message);
        }
    };

    this.getInterface = function() {
        return this.interface;
    };
    
    this.create();
};

exports.create = function() {
    return new ToolkitObject();
};

exports.getInterface = function() {
    return exports.create().getInterface();
};

exports.sendClick = function(wName, x, y, retry) {
    var i = 0;
    while (i < retry) {
        exports.getInterface().SendClick(wName, x, y);
        i++;
    }
};

exports.sendKeys = function(wName, s) {
    return exports.getInterface().SendKeys(wName, s);
};

// https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html

exports.alert = function(message) {
    return exports.getInterface().Alert(message);
};

exports.confirm = function(message) {
    return exports.getInterface().Confirm(message);
};

exports.prompt = function(message, _default) {
    return exports.getInterface().Prompt(message, _default);
};