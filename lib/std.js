//////////////////////////////////////////////////////////////////////////////////
//
//    std.js
//
//    Common routines.  Defines LIB object which contains the API, as well as
//    a global DBG function.
//
//  References
//  * https://github.com/redskyit/wsh-appjs
//  * https://github.com/JSman-/JS-Framework
//
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Global APIs
/////////////////////////////////////////////////////////////////////////////////

Function.prototype.GetResource = function(ResourceName)
{
    if (!this.Resources) 
    {
        var UnNamedResourceIndex = 0, _this = this;
        this.Resources = {};
        
        function f(match, resType, Content)
        {
            _this.Resources[(resType=="[[")?UnNamedResourceIndex++:resType.slice(1,-1)] = Content; 
        }
        this.toString().replace(/\/\*(\[(?:[^\[]+)?\[)((?:[\r\n]|.)*?)\]\]\*\//gi, f);
    }
    
    return this.Resources[ResourceName];
}

global.GetResource = function(ResourceName)
{
    return arguments.callee.caller.GetResource(ResourceName);
}

global.sleep = function(ms, callback) {
    WScript.Sleep(ms);
    if(typeof(callback) == "function") {
        callback();
    }
}

global.exit = function() {
    WScript.Quit();
}

if (!('toArray' in Enumerator.prototype)) {
    Enumerator.prototype.toArray = function() {
        var Result = [];
        for (;!this.atEnd();this.moveNext())
        Result.push(this.item())
        return Result;
    }
}

if (!('forEach' in Enumerator.prototype)) {
    Enumerator.prototype.forEach = function(action, that /*opt*/) {
        this.toArray().forEach(action, that);
    }
}

// Add ECMA262-5 method binding if not supported natively
if (!('bind' in Function.prototype)) {
    Function.prototype.bind = function(owner) {
        var that= this;
        if (arguments.length<=1) {
            return function() {
                return that.apply(owner, arguments);
            };
        } else {
            var args= Array.prototype.slice.call(arguments, 1);
            return function() {
                return that.apply(owner, arguments.length===0? args : args.concat(Array.prototype.slice.call(arguments)));
            };
        }
    };
}

// Add ECMA262-5 string trim if not supported natively
//
if (!('trim' in String.prototype)) {
    String.prototype.trim = function() {
        return this.replace(/^\s+/, '').replace(/\s+$/, '');
    };
}

// Add ECMA262-5 Array methods if not supported natively
//
if (!('indexOf' in Array.prototype)) {
    Array.prototype.indexOf = function(find, i /*opt*/) {
        if (i===undefined) i= 0;
        if (i<0) i+= this.length;
        if (i<0) i= 0;
        for (var n= this.length; i<n; i++)
        if (this[i]===find) return i;
            //if (i in this && this[i]===find) return i;
        return -1;
    };
}

if (!('lastIndexOf' in Array.prototype)) {
    Array.prototype.lastIndexOf = function(find, i /*opt*/) {
        if (i===undefined) i= this.length-1;
        if (i<0) i+= this.length;
        if (i>this.length-1) i= this.length-1;
        for (i++; i-->0;) /* i++ because from-argument is sadly inclusive */
           // if (i in this && this[i]===find)
            if (this[i]===find)
            return i;
        return -1;
    };
}

if (!('forEach' in Array.prototype)) {
    Array.prototype.forEach = function(action, that /*opt*/) {
        for (var i= 0, n= this.length; i<n; i++)
            //if (i in this)
                action.call(that, this[i], i, this);
    };
}

if (!('map' in Array.prototype)) {
    Array.prototype.map = function(mapper, that /*opt*/) {
        var other= new Array(this.length);
        for (var i= 0, n= this.length; i<n; i++)
            //if (i in this)
                other[i]= mapper.call(that, this[i], i, this);
        return other;
    };
}

if (!('filter' in Array.prototype)) {
    Array.prototype.filter = function(filter, that /*opt*/) {
        var other= [], v;
        for (var i=0, n= this.length; i<n; i++)
            //if (i in this && filter.call(that, v= this[i], i, this))
        if (filter.call(that, v= this[i], i, this))
                other.push(v);
        return other;
    };
}

if (!('every' in Array.prototype)) {
    Array.prototype.every = function(tester, that /*opt*/) {
        for (var i= 0, n= this.length; i<n; i++)
            //if (i in this && !tester.call(that, this[i], i, this))
        if (!tester.call(that, this[i], i, this))
                return false;
        return true;
    };
}

if (!('some' in Array.prototype)) {
    Array.prototype.some = function(tester, that /*opt*/) {
        for (var i= 0, n= this.length; i<n; i++)
            //if (i in this && tester.call(that, this[i], i, this))
        if (tester.call(that, this[i], i, this))
                return true;
        return false;
    };
}

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

var module = { global: global, require: global.require };
module.VERSIONINFO = "Standard Lib (std.js) version 0.2";

/////////////////////////////////////////////////////////////////////////////////
// Emulate Server.CreateObject
/////////////////////////////////////////////////////////////////////////////////

module.CreateObject = function(n) { return new ActiveXObject(n); };

/////////////////////////////////////////////////////////////////////////////////

return module;
