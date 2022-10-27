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
                    this.__intervals__[i].cleared = true;
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
                    this.__timeouts__[i].cleared = true;
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
                'timestamp': Date.now(),
                'cleared': false,
                'callback': null
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
                'timestamp': Date.now(),
                'cleared': false
            });
            return this.__lastTimeoutID__;
        } else {
            return window.setTimeout.apply(null, arguments);
        }
    };

    this.__setIntervalWithCallback__ = function(code, callback) {
        this.__lastIntervalID__++;
        this.__intervals__.push({
            'id': this.__lastIntervalID__,
            'code': code,
            'delay': delay,
            'arguments': [],
            'timestamp': Date.now(),
            'cleared': false,
            'callback': callback
        });
        return this.__lastIntervalID__;
    };

    this.__getIntervals__ = function() {
        var intervals = [];
        var cur = Date.now();

        for (var i = 0; i < this.__intervals__.length; i++) {
            if (!this.__intervals__[i].cleared && (this.__intervals__[i].timestamp + this.__intervals__[i].delay) >= cur) {
                intervals.push(this.__intervals__[i]);
                this.__intervals__[i].timestamp = cur;
                if (typeof this.__intervals__[i].callback === "function") {
                    this.__intervals__[i].delay = this.__intervals__[i].callback();
                }
            }
        }

        return intervals;
    };

    this.__getTimeouts__ = function() {
        var timeouts = [];
        var cur = Date.now();
        
        for (var i = 0; i < this.__timeouts__.length; i++) {
            if (!this.__timeouts__[i].cleared && (this.__timeouts__[i].timestamp + this.__timeouts__[i].delay) >= cur) {
                timeouts.push(this.__timeouts__[i]);
                this.__timeouts__[i].cleared = true;
            }
        }
        
        return timeouts;
    };
}

exports.create = function() {
    return new FakeWorker();
};

exports.VERSIONINFO = "FakeWorker module (fakeworker.js) version 0.0.1";
exports.AUTHOR = "catswords@protonmail.com";
exports.global = global;
exports.require = global.require;
