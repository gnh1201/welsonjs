var BASE64 = require("lib/base64");

function FakeWorker() {
    this.__lastIntervalID__ = 0;
    this.__lastTimeoutID__ = 0;
    this.__intervals__ = [];
    this.__timeouts__ = [];
    this.__isWindow__ = (typeof window !== "undefined");

    this.atob = function(encodedData) {
        if (!this.__isWindow__) {
            return BASE64.decode(encodedData);
        } else {
            return window.atob.apply(null, arguments);
        }
    };

    this.btoa = function(stringToEncode) 
        if (!this.__isWindow__) {
            return BASE64.encode(stringToEncode);
        } else {
            return window.btoa.apply(null, arguments);
        }
    };

    this.clearInterval = function(intervalID) {
        if (!this.__isWindow__) {
            for (var i = 0; i < this.__intervals__.length; i++) {
                if (this.__intervals__[i].id == intervalID) {
                    delete this.__intervals__[i];
                    break;
                }
            }
        } else {
            return this.clearInterval.apply(null, arguments);
        }
    };

    this.clearTimeout = function(timeoutID) {
        if (!this.__isWindow__) {
            for (var i = 0; i < this.__timeouts__.length; i++) {
                if (this.__timeouts__[i].id == timeoutID) {
                    delete this.__timeouts__[i];
                    break;
                }
            }
        } else {
            return this.clearTimeout.apply(null, arguments);
        }
    };

    this.setInterval = function(code, delay) {
        if (!this.__isWindow__) {
            this.__lastIntervalID__++;
            this.__intervals__.push({
                'id': this.__lastIntervalID__,
                'code': code,
                'delay': delay,
                'arguments': (arguments.length > 2 ? arguments.slice(2) : []),
                'timestamp': Date.now()
            });
            return this.__lastIntervalID__;
        } else {
            return window.setInterval.apply(null, arguments);
        }
    };

    this.setTimeout = function(code, delay) {
        if (!this.__isWindow__) {
            this.__lastTimeoutID__++;
            this.__timeouts__.push({
                'id': this.__lastTimeoutID__,
                'code': code,
                'delay': delay,
                'arguments': (arguments.length > 2 ? arguments.slice(2) : []),
                'timestamp': Date.now()
            });
            return this.__lastTimeoutID__;
        } else {
            return window.setTimeout.apply(null, arguments);
        }
    };
}

exports.create = function() {
    return new FakeWorker();
};

exports.VERSIONINFO = "FakeWorker module (fakeworker.js) version 0.0.1";
exports.AUTHOR = "catswords@protonmail.com";
exports.global = global;
exports.require = global.require;
