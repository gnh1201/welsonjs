// toolkit.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

function ToolkitObject() {
    this.interface = null;

    this.create = function() {
        try {
            this.interface = CreateObject("WelsonJS.Toolkit");
        } catch (e) {
            console.info("WelsonJS.Toolkit not installed");
            console.info("It could be download from https://github.com/gnh1201/welsonjs");
            console.error(e.message);
        }
        return this;
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

// [lib/toolkit] Implementation of User prompts (alert, confirm. prompt) #21
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

// [Toolkit] Access to a shared memory #96
function NamedSharedMemory(name) {
    var Toolkit = create().getInterface();

    this.name = name;
    this.isInitialized = false;

    this.open = function() {
        this.isInitialized = Toolkit.OpenNamedSharedMemory(this.name);
    };
    
    this.readText = function() {
        return Toolkit.ReadTextFromSharedMemory(this.name);
    };
    
    this.writeText = function(text) {
        Toolkit.WriteTextToSharedMemory(this.name, text);
    };

    this.clear = function() {
        Toolkit.ClearSharedMemory(this.name);
    };
}

exports.create = create;
exports.getInterface = getInterface;
exports.sendClick = sendClick;
exports.sendKeys = sendKeys;
exports.sendFnKey = sendFnKey;
exports.alert = alert;
exports.confirm = confirm;
exports.prompt = prompt;
exports.NamedSharedMemory = NamedSharedMemory;

exports.VERSIONINFO = "WelsonJS.Toolkit Native API version 0.3.2";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
