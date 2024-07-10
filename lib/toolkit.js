// toolkit.js
// WelsonJS native component interface for WelsonJS framework
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

function ToolkitObject() {
    this._interface = null;

    this.create = function() {
        try {
            this._interface = CreateObject("WelsonJS.Toolkit");
        } catch (e) {
            console.info("WelsonJS.Toolkit not installed");
            console.info("It could be download from https://github.com/gnh1201/welsonjs");
            console.error(e.message);
        }
        return this;
    };

    this.getInterface = function() {
        return this._interface;
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
    var _interface = create().getInterface();

    this.name = name;

    this.writeText = function(text) {
        return _interface.WriteTextToSharedMemory(this.name, text);
    };

    this.readText = function() {
        return _interface.ReadTextFromSharedMemory(this.name);
    };
    
    this.clear = function() {
        return _interface.ClearSharedMemory(this.name);
    };
    
    this.close = function() {
        return _interface.CloseSharedMemory(this.name);
    };
}

function openProcess(filepath) {
    return getInterface().OpenProcess(filepath);
}

function closeProcess(pid) {
    return getInterface().CloseProcess(pid);
}

function encryptStringHIGHT(userKey, data) {
    return getInterface().EncryptStringHIGHT(userKey, data);
}

function decryptStringHIGHT(userKey, encryptedData) {
    return getInterface().DecryptStringHIGHT(userKey, encryptedData);
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
exports.openProcess = openProcess;
exports.closeProcess = closeProcess;
exports.encryptStringHIGHT = encryptStringHIGHT;
exports.decryptStringHIGHT = decryptStringHIGHT;

exports.VERSIONINFO = "WelsonJS native component interface (WelsonJS.Toolkit) version 0.3.6";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
