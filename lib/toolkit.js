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

    this.checkIsEnabled = function() {
        return (this.interface != null);
    };
    
    this.sendClick = function(wName, x, y, retry) {
		if (!this.checkIsEnabled())
			return;

        var i = 0;
        while (i < retry) {
            this.interface.SendClick(wName, x, y);
            i++;
        }
    };

    this.sendKey = function(wName, s) {
        if (!this.checkIsEnabled())
            return;

        return this.interface.SendKeys(wName, s);
    };
    
    // https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html

    this.alert = function(message) {
        if (!this.checkIsEnabled())
            return;

        return this.interface.Alert(message);
    };
    
    this.confirm = function(message) {
        if (!this.checkIsEnabled())
            return;

        return this.interface.Confirm(message);
    };
    
    this.prompt = function(message, _default) {
        if (!this.checkIsEnabled())
            return;

        return this.interface.Prompt(message, _default);
    };

    this.create();
};

var instance = new ToolkitObject();

exports.sendClick = instance.sendClick;
exports.sendKeys = instance.sendKeys;
exports.alert = instance.alert;
exports.confirm = instance.confirm;
exports.prompt = instance.prompt;

exports.VERSIONINFO = "WelsonJS Toolkit (toolkit.js) version 0.2";
exports.global = global;
exports.require = global.require;
