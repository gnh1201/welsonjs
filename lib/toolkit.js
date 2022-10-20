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

function create() {
    return new ToolkitObject();
}

function getInterface() {
    return create().getInterface();
}

function sendClick(wName, x, y, retry) {
    var i = 0;
    while (i < retry) {
        getInterface().SendClick(wName, x, y);
        i++;
    }
}

function sendKeys(wName, s) {
    return getInterface().SendKeys(wName, s);
}

function sendFnKey(wName, num) {
    return getInterface().SendFnKey(wName, num);
}

// https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html

function alert(message) {
    return getInterface().Alert(message);
}

function confirm(message) {
    return getInterface().Confirm(message);
}

function prompt(message, _default) {
    return getInterface().Prompt(message, _default);
}

exports.create = create;
exports.getInterface = getInterface;
exports.sendClick = sendClick;
exports.sendKeys = sendKeys;
exports.sendFnKey = sendFnKey;
exports.alert = alert;
exports.confirm = confirm;
exports.prompt = prompt;

exports.VERSIONINFO = "WelsonJS.Toolkit API version 0.3";
exports.AUTHOR = "catswords@protonmail.com";
exports.global = global;
exports.require = global.require;
