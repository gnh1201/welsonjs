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
        }
    };

    this.getInterface = function() {
        return this.interface;
    };
	
	this.create();
};

var Toolkit = new ToolkitObject();

exports.create = function() {
    return new ToolkitObject();
};

exports.getInterface = function() {
	return Toolkit.getInterface();
};

exports.sendClick = function(wName, x, y, repeat) {
	var i = 0;
	while (i < repeat) {
		Toolkit.getInterface().SendClick(wName, x, y);
		i++;
	}
};

exports.sendKeys = function(wName, s) {
	return Toolkit.getInterface().SendKeys(wName, s);
};

// https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html

exports.alert = function(message) {
    return Toolkit.getInterface().Alert(message);
};

exports.confirm = function(message) {
    return Toolkit.getInterface().Confirm(message);
};

exports.prompt = function(message, _default) {
    return Toolkit.getInterface().Prompt(message, _default);
};
