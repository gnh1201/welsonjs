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

    this.btoa = function(stringToEncode) {
        if (!this.__isWindow__) {
            return BASE64.encode(stringToEncode);
        } else {
            return window.btoa.apply(null, arguments);
        }
    };

    this.clearInterval = function(intervalID) {
        for (var i = 0; i < this.__intervals__.length; i++) {
            if (this.__intervals__[i].id == intervalID) {
                this.__intervals__[i].cleared = true;
                break;
            }
        }
    };

    this.clearTimeout = function(timeoutID) {
        for (var i = 0; i < this.__timeouts__.length; i++) {
            if (this.__timeouts__[i].id == timeoutID) {
                this.__timeouts__[i].cleared = true;
            }
        }
    };

    this.setInterval = function(code, delay) {
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
    };

    this.setTimeout = function(code, delay) {
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
    };

    this.setIntervalWithCallback = function(code, callback) {
        this.__lastIntervalID__++;
        this.__intervals__.push({
            'id': this.__lastIntervalID__,
            'code': code,
            'delay': 0,    // replace to the callback
            'arguments': [],
            'timestamp': Date.now(),
            'cleared': false,
            'callback': callback
        });
        return this.__lastIntervalID__;
    };

    this.getIntervals = function() {
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
            cur = Date.now();
        }

        return intervals;
    };

    this.getTimeouts = function() {
        var timeouts = [];
        var cur = Date.now();

        for (var i = 0; i < this.__timeouts__.length; i++) {
            if (!this.__timeouts__[i].cleared && (this.__timeouts__[i].timestamp + this.__timeouts__[i].delay) >= cur) {
                timeouts.push(this.__timeouts__[i]);
                this.__timeouts__[i].cleared = true;
            }
            cur = Date.now();
        }
        
        return timeouts;
    };
}

exports.create = function() {
    return new FakeWorker();
};

exports.repeat = function(target, worker, onError) {
    if (!(worker instanceof FakeWorker)) return;

    if (["number", "boolean"].indexOf(typeof target) > -1) {
        var ms = target;
        var cur = Date.now();
        var end = cur + ms;

        while (ms === true ? true : (cur < end)) {
            var intervals = worker.getIntervals();
            for (var i = 0; i < intervals.length; i++) {
                try {
                    if (typeof intervals[i].code === "function") intervals[i].code(i);
                } catch (e) {
                    if (typeof onError === "function") onError(e, i);
                }
                cur = Date.now();
            };
        }

        end = cur;
    }

    return end;
};

exports.VERSIONINFO = "FakeWorker module (fakeworker.js) version 0.0.4";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
