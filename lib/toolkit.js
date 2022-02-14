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
	
	this.sendClick = function(wName, x, y, retry) {
		var i = 0;
		while (i < retry) {
			this.interface.SendClick(wName, x, y);
			i++;
        }
    };
    
    this.sendKeys = function(wName, s) {
        return this.interface.SendKeys(wName, s);
    };
	
	// https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html
	
	this.alert = function(message) {
		return this.interface.Alert(message);
	};
    
    this.confirm = function(message) {
        return this.interface.Confirm(message);
    };
    
    this.prompt = function(message, _default) {
        return this.interface.Prompt(message, _default);
    };

    this.create();
};

var Toolkit = new ToolkitObject();

exports.create = Toolkit.create();
exports.sendClick = Toolkit.sendClick;
exports.sendKeys = Toolkit.sendKeys;
exports.alert = Toolkit.alert;
exports.confirm = Toolkit.confirm;
exports.prompt = Toolkit.prompt;
