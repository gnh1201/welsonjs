// is.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Type, presence, arithmetic, format, string, date, object and array checks.
//
// This module replaces the third-party is.js 0.9.0 that app.js used to publish as the
// global "is". That library was removed for CVE-2020-26302, a ReDoS in is.url(), and it
// has no patched release. The method names and the meaning of the calls that
// applications make are kept, so an application written against is.js 0.9.0 does not
// have to change, but no third-party library is loaded or bundled here: the checks are
// written in this file, with no syntax beyond ES3, and no regular expression that can
// backtrack catastrophically. The patterns for the small formats (postal codes, phone
// numbers, card numbers and the like) are the usual definitions of those formats, the
// same shapes the is.js documentation listed for them.
//
// is.url() and is.email() do not use a regular expression at all. They are parsed once,
// from left to right, over a length limited value, so a call costs time proportional to
// the length of its input and a hostile value (a long run of "a", "@", ":" or "//") can
// not hold the engine (CVE-2020-26302). The other format checks use anchored patterns
// without nested unbounded quantifiers, and every value passed to them is length limited.
//
// Provided: every method of is.js 0.9.0 except the browser and device detection group
// (android, androidPhone, androidTablet, blackberry, chrome, desktop, edge, firefox, ie,
// ios, ipad, iphone, ipod, mobile, opera, operaMini, phantom, safari, tablet,
// touchDevice, windowsPhone, windowsTablet), which reads navigator.userAgent and means
// nothing in Windows Script Host. Use lib/browser.js for that in an embedded browser.
// is.windows(), is.linux(), is.mac(), is.online() and is.offline() are provided: they
// answer for the host the script runs on, not for a browser's user agent. In Windows
// Script Host, is.online() is true when an enabled network adapter has a default
// gateway (read through WMI), and true when that cannot be read.
//
// Differences from is.js 0.9.0, all deliberate:
// - is.url() accepts a URL whose host is a loopback, private or link-local IPv4 address
//   (is.js 0.9.0 rejected 10.x, 127.x, 169.254.x, 192.168.x and 172.16-31.x), accepts a
//   port of one digit and up to 65535 (is.js allowed 2 to 5 digits), and accepts a
//   bracketed IPv6 host. A value longer than 2048 characters is not a URL.
// - is.email() accepts ASCII only, one "@", and a local part of up to 64 characters with
//   no leading, trailing or doubled dot. A value longer than 254 characters is not an
//   address. is.js 0.9.0 allowed the non-ASCII ranges of RFC 6531 and quoted local parts.
// - is.numeric() (alias is.numericString()) is provided; is.js 0.9.0 had no such method.
// - is.fn() is an alias of is["function"], is.ssn() of is.socialSecurityNumber().
// - is.finite() does not coerce its argument (is.js 0.9.0 delegated to the global
//   isFinite, so is.finite("3") was true there and is false here).
// - is.empty(), is.propertyCount() and is.equal() work the same way, but a check that
//   receives a value of the wrong type returns false instead of throwing a TypeError
//   (is.include(null, "a") threw in is.js 0.9.0).
// - is.setRegexp() replaces or adds a check for any method name; in is.js 0.9.0 it only
//   replaced a name that was already a built-in pattern.
// - is.windows(), is.linux(), is.mac() answer for the host platform, and is.VERSION is
//   "1.0.0" (the version of this module), not "0.9.0".
/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

var is = {};

is.VERSION = "1.0.0";

is.not = {};
is.all = {};
is.any = {};

var objectToString = Object.prototype.toString;
var hasOwnProperty = Object.prototype.hasOwnProperty;
var arraySlice = Array.prototype.slice;

// A longer value is not a URL or an email address. The limits also bound the work those
// checks do, together with their single-pass parsing.
var MAX_URL_LENGTH = 2048;
var MAX_EMAIL_LENGTH = 254;

// Upper bound for a value handed to a regular expression, so that no pattern is ever
// asked to work on unbounded input.
var MAX_REGEXP_VALUE_LENGTH = 8192;

// The [[Class]] of a value. An engine that refuses null or undefined in
// Object.prototype.toString still has to answer for them.
function getClass(value) {
    try {
        return objectToString.call(value);
    } catch (e) {
        return typeof value === "undefined" ? "[object Undefined]" : "[object Null]";
    }
}

// Reverses the sense of a predicate.
function not(func) {
    return function() {
        return !func.apply(null, arraySlice.call(arguments));
    };
}

// Calls a predicate for each parameter and returns true only if all of them pass.
function all(func) {
    return function() {
        var params = getParams(arguments);
        var length = params.length;
        for (var i = 0; i < length; i++) {
            if (!func.call(null, params[i])) {
                return false;
            }
        }
        return true;
    };
}

// Calls a predicate for each parameter and returns true if any of them pass.
function any(func) {
    return function() {
        var params = getParams(arguments);
        var length = params.length;
        for (var i = 0; i < length; i++) {
            if (func.call(null, params[i])) {
                return true;
            }
        }
        return false;
    };
}

// Collects the parameters of an interface call. A single array parameter is the list.
function getParams(args) {
    var params = arraySlice.call(args);
    if (params.length === 1 && is.array(params[0])) {
        params = params[0];
    }
    return params;
}

// Comparison predicates used by is.above(), is.under(), is.within() and is.sorted().
var comparator = {
    "<": function(a, b) { return a < b; },
    "<=": function(a, b) { return a <= b; },
    ">": function(a, b) { return a > b; },
    ">=": function(a, b) { return a >= b; }
};

// The text of a value, or null when it has none to check. The coercion is the one a
// regular expression made in is.js 0.9.0 (a String object and a number become text).
function getCheckableText(value) {
    if (value === null || typeof value === "undefined") {
        return null;
    }
    if (typeof value === "string") {
        return value;
    }
    try {
        return "" + value;
    } catch (e) {
        return null;
    }
}

function isDigitCode(code) {
    return code >= 48 && code <= 57;
}

function isLowerAlphaCode(code) {
    return code >= 97 && code <= 122;
}

function isUpperAlphaCode(code) {
    return code >= 65 && code <= 90;
}

function isAlphaCode(code) {
    return isLowerAlphaCode(code) || isUpperAlphaCode(code);
}

function isAlphaNumericCode(code) {
    return isAlphaCode(code) || isDigitCode(code);
}

// A letter of a host name, including the letters of a non-ASCII (IDN) name.
function isNameCode(code) {
    return isAlphaCode(code) || code > 127;
}

function isHexadecimalCode(code) {
    return isDigitCode(code) || (code >= 97 && code <= 102) || (code >= 65 && code <= 70);
}

// A space or a control character; no URL part may contain one.
function isSpaceOrControlCode(code) {
    return code <= 32 || code === 127;
}

function everyCharCode(text, predicate) {
    for (var i = 0; i < text.length; i++) {
        if (!predicate(text.charCodeAt(i))) {
            return false;
        }
    }
    return true;
}

function containsSpaceOrControl(text) {
    return !everyCharCode(text, function(code) {
        return !isSpaceOrControlCode(code);
    });
}

function isAllDigits(text) {
    return text.length > 0 && everyCharCode(text, isDigitCode);
}

// A DNS label: 1 to 63 letters, digits or hyphens, not starting or ending with a hyphen.
// A non-ASCII character is allowed, so an internationalized name is accepted as written
// (is.js 0.9.0 allowed it too); the name is not converted to punycode here.
function isValidDnsLabel(label) {
    if (label.length < 1 || label.length > 63) {
        return false;
    }
    if (label.charAt(0) === "-" || label.charAt(label.length - 1) === "-") {
        return false;
    }
    for (var i = 0; i < label.length; i++) {
        var code = label.charCodeAt(i);
        if (!isAlphaNumericCode(code) && code !== 45 && code < 128) {
            return false;
        }
    }
    return true;
}

// A dotted quad in base 10, each part 0 to 255, without a leading zero.
function isValidIpv4(value) {
    var parts = value.split(".");
    if (parts.length !== 4) {
        return false;
    }
    for (var i = 0; i < 4; i++) {
        var part = parts[i];
        if (part.length < 1 || part.length > 3 || !isAllDigits(part)) {
            return false;
        }
        if (part.length > 1 && part.charAt(0) === "0") {
            return false;
        }
        var octet = parseInt(part, 10);
        if (!(octet >= 0 && octet <= 255)) {
            return false;
        }
    }
    return true;
}

// Eight groups of one to four hexadecimal digits, with one "::" allowed to stand for the
// groups that are left out, and an IPv4 address allowed as the last two groups. Parsed
// once from left to right; the pattern is.js 0.9.0 used had lookaheads and backreferences.
function isValidIpv6(value) {
    if (value.length < 2 || value.length > 45) {
        return false;
    }
    if (value.charAt(0) === ":" && value.charAt(1) !== ":") {
        return false;
    }
    if (value.charAt(value.length - 1) === ":" && value.charAt(value.length - 2) !== ":") {
        return false;
    }

    var position = value.indexOf("::");
    if (position > -1 && value.indexOf("::", position + 2) > -1) {
        return false;
    }

    var left = position === -1 ? value : value.substring(0, position);
    var right = position === -1 ? "" : value.substring(position + 2);
    var groups = (left === "" ? [] : left.split(":")).concat(right === "" ? [] : right.split(":"));
    // An IPv4 address can only be the last 32 bits: the last group of an address
    // without "::", or the last group after it. "1.2.3.4::" has it before the "::".
    var ipv4TailAllowed = position === -1 || right !== "";

    var count = 0;
    for (var i = 0; i < groups.length; i++) {
        var group = groups[i];
        if (group.length === 0) {
            return false;
        }
        if (i === groups.length - 1 && group.indexOf(".") > -1) {
            if (!ipv4TailAllowed || !isValidIpv4(group)) {
                return false;
            }
            count += 2;
        } else {
            if (group.length > 4 || !everyCharCode(group, isHexadecimalCode)) {
                return false;
            }
            count += 1;
        }
    }

    // "::" stands for at least one group; without it the address has exactly eight.
    return position === -1 ? count === 8 : count < 8;
}

// The host of a URL or the domain of an email address: an IPv4 address when one is
// allowed, or a dotted name whose last label is an alphabetic or punycode top level
// domain. An email address is written with a dotted name, as in is.js 0.9.0.
function isValidHostName(host, allowIpv4Host) {
    if (host.length < 1 || host.length > 253) {
        return false;
    }
    if (allowIpv4Host && isValidIpv4(host)) {
        return true;
    }

    var labels = host.split(".");
    if (labels.length < 2) {
        return false;
    }
    for (var i = 0; i < labels.length; i++) {
        if (!isValidDnsLabel(labels[i])) {
            return false;
        }
    }

    var topLevelDomain = labels[labels.length - 1];
    if (topLevelDomain.length < 2 || isAllDigits(topLevelDomain)) {
        return false;
    }
    if (topLevelDomain.substring(0, 4).toLowerCase() === "xn--") {
        return true;
    }
    return everyCharCode(topLevelDomain, isNameCode);
}

function isValidPort(port) {
    if (port.length < 1 || port.length > 5 || !isAllDigits(port)) {
        return false;
    }
    var number = parseInt(port, 10);
    return number >= 1 && number <= 65535;
}

// The user part of a URL before "@": the unreserved and sub-delimiter characters of
// RFC 3986, with ":" allowed once or more for a password.
function isValidUserInfo(userInfo) {
    if (userInfo.length === 0) {
        return false;
    }
    var allowed = "-._~%!$&'()*+,;=:";
    for (var i = 0; i < userInfo.length; i++) {
        var code = userInfo.charCodeAt(i);
        if (!isAlphaNumericCode(code) && allowed.indexOf(userInfo.charAt(i)) < 0) {
            return false;
        }
    }
    return true;
}

// The position of a ":" that could start a scheme name, or -1 when the value has none
// before its first "/", "?" or "#".
function indexOfSchemeSeparator(value) {
    for (var i = 0; i < value.length; i++) {
        var chr = value.charAt(i);
        if (chr === ":") {
            return i;
        }
        if (chr === "/" || chr === "?" || chr === "#") {
            return -1;
        }
    }
    return -1;
}

// is.url() without a regular expression: one pass over a value of bounded length.
function isValidUrl(value) {
    if (value.length < 1 || value.length > MAX_URL_LENGTH) {
        return false;
    }
    if (containsSpaceOrControl(value)) {
        return false;
    }

    // A scheme counts only as one of the three this check knows, written with "//"
    // before the authority. Anything else is read as an authority, so "mailto:user" and
    // "javascript:void(0)" fail on their host or port rather than being taken as a scheme.
    var position = 0;
    var hasScheme = false;
    var schemeSeparator = indexOfSchemeSeparator(value);
    if (schemeSeparator > -1 && value.substring(schemeSeparator, schemeSeparator + 3) === "://") {
        var scheme = value.substring(0, schemeSeparator).toLowerCase();
        if (scheme !== "http" && scheme !== "https" && scheme !== "ftp") {
            return false;
        }
        hasScheme = true;
        position = schemeSeparator + 3;
    }

    // The authority runs up to the path, the query or the fragment.
    var authorityEnd = value.length;
    for (var i = position; i < value.length; i++) {
        var chr = value.charAt(i);
        if (chr === "/" || chr === "?" || chr === "#") {
            authorityEnd = i;
            break;
        }
    }

    var authority = value.substring(position, authorityEnd);
    if (authority.length === 0) {
        return false;
    }

    var hostAndPort = authority;
    var at = authority.lastIndexOf("@");
    if (at > -1) {
        // A user part belongs to a URL written with a scheme; without one an "@" leaves
        // the authority without a host ("user@example.com" is an address, not a URL).
        if (!hasScheme || !isValidUserInfo(authority.substring(0, at))) {
            return false;
        }
        hostAndPort = authority.substring(at + 1);
    }

    var host = hostAndPort;
    if (hostAndPort.charAt(0) === "[") {
        var close = hostAndPort.indexOf("]");
        if (close < 0) {
            return false;
        }
        if (!isValidIpv6(hostAndPort.substring(1, close))) {
            return false;
        }
        var rest = hostAndPort.substring(close + 1);
        if (rest !== "") {
            if (rest.charAt(0) !== ":" || !isValidPort(rest.substring(1))) {
                return false;
            }
        }
        return true;
    }

    var colon = hostAndPort.lastIndexOf(":");
    if (colon > -1) {
        host = hostAndPort.substring(0, colon);
        if (!isValidPort(hostAndPort.substring(colon + 1))) {
            return false;
        }
    }

    return isValidHostName(host, true);
}

// is.email() without a regular expression.
function isValidEmailAddress(value) {
    if (value.length < 3 || value.length > MAX_EMAIL_LENGTH) {
        return false;
    }
    if (containsSpaceOrControl(value)) {
        return false;
    }

    // One "@" only; the quoted local parts of RFC 5322 that may contain one are not
    // accepted, and neither is a non-ASCII address of RFC 6531.
    var at = value.lastIndexOf("@");
    if (at < 1 || at !== value.indexOf("@") || at === value.length - 1) {
        return false;
    }

    var localPart = value.substring(0, at);
    var domain = value.substring(at + 1);

    if (localPart.length > 64) {
        return false;
    }
    if (localPart.charAt(0) === "." || localPart.charAt(localPart.length - 1) === "." ||
            localPart.indexOf("..") > -1) {
        return false;
    }

    var allowed = "!#$%&'*+-/=?^_`{|}~.";
    for (var i = 0; i < localPart.length; i++) {
        var code = localPart.charCodeAt(i);
        if (!isAlphaNumericCode(code) && allowed.indexOf(localPart.charAt(i)) < 0) {
            return false;
        }
    }

    // The domain is ASCII too: isValidHostName() accepts a Unicode name, as a URL
    // host may be one, but an address of RFC 6531 is not accepted here.
    for (var j = 0; j < domain.length; j++) {
        if (domain.charCodeAt(j) > 127) {
            return false;
        }
    }

    return isValidHostName(domain, false);
}

// is.numeric() without a regular expression: an optional sign, digits, and at most one
// decimal point with at least one digit on one of its sides.
function isNumericText(value) {
    var i = value.charAt(0) === "-" || value.charAt(0) === "+" ? 1 : 0;
    var digits = 0;
    var points = 0;

    for (; i < value.length; i++) {
        var chr = value.charAt(i);
        if (chr === ".") {
            points += 1;
            if (points > 1) {
                return false;
            }
        } else if (isDigitCode(value.charCodeAt(i))) {
            digits += 1;
        } else {
            return false;
        }
    }

    return digits > 0;
}

// The number of own properties of an object, in the sense is.empty() needs: an array
// counts its length and an arguments object its callee.
function countOwnProperties(value) {
    if (typeof Object.getOwnPropertyNames === "function") {
        return Object.getOwnPropertyNames(value).length;
    }

    // Legacy engine: count the enumerable properties, then the ones a collection keeps
    // out of an enumeration.
    var count = 0;
    for (var property in value) {
        if (hasOwnProperty.call(value, property)) {
            count += 1;
        }
    }
    if (typeof value.length === "number") {
        count += 1;
    }
    if (is.arguments(value)) {
        count += 1;
    }
    return count;
}

// The platform the script runs on, for is.windows(), is.linux() and is.mac().
function getHostPlatform() {
    try {
        if (typeof navigator !== "undefined" && navigator && typeof navigator.platform === "string") {
            return navigator.platform.toLowerCase();
        }
    } catch (e) {
        // No navigator object in this host.
    }
    if (typeof WScript !== "undefined" && WScript && typeof WScript.FullName === "string") {
        return "win32";
    }
    return "";
}

/////////////////////////////////////////////////////////////////////////////////
// Type checks
/////////////////////////////////////////////////////////////////////////////////

// is a given value Arguments?
is.arguments = function(value) {    // fallback check is for IE
    return getClass(value) === "[object Arguments]" ||
        (value !== null && typeof value === "object" && "callee" in value);
};

// is a given value Array?
is.array = (typeof Array.isArray === "function") ? function(value) {
    return Array.isArray(value);
} : function(value) {
    return getClass(value) === "[object Array]" || value instanceof Array;
};

// is a given value Boolean?
is["boolean"] = function(value) {
    return value === true || value === false || getClass(value) === "[object Boolean]";
};

// is a given value Char?
is["char"] = function(value) {
    return is.string(value) && value.length === 1;
};

// is a given value Date Object?
is.date = function(value) {
    return getClass(value) === "[object Date]" || value instanceof Date;
};

// is a given object a DOM node?
is.domNode = function(object) {
    return is.object(object) && object.nodeType > 0;
};

// is a given value Error object?
is.error = function(value) {
    return getClass(value) === "[object Error]" || value instanceof Error;
};

// is a given value function?
is["function"] = function(value) {    // fallback check is for IE
    return getClass(value) === "[object Function]" || typeof value === "function";
};

// is given value a pure JSON object?
is.json = function(value) {
    return getClass(value) === "[object Object]";
};

// is a given value NaN?
is.nan = function(value) {
    return typeof value === "number" && value !== value;
};

// is a given value null?
is["null"] = function(value) {
    return value === null;
};

// is a given value number? (a finite one, as in is.js 0.9.0)
is.number = function(value) {
    return typeof value === "number" && value === value &&
        value !== Infinity && value !== -Infinity;
};

// is a given value object?
is.object = function(value) {
    return value !== null && typeof value !== "undefined" &&
        (typeof value === "object" || typeof value === "function");
};

// is a given value RegExp?
is.regexp = function(value) {
    return getClass(value) === "[object RegExp]" || value instanceof RegExp;
};

// are given values same type? (NaN and Number are not the same type)
is.sameType = function(value, other) {
    var tag = getClass(value);
    if (tag !== getClass(other)) {
        return false;
    }
    if (tag === "[object Number]") {
        return !is.any.nan(value, other) || is.all.nan(value, other);
    }
    return true;
};
// sameType method does not support 'all' and 'any' interfaces
is.sameType.api = ["not"];

// is a given value String?
is.string = function(value) {
    return typeof value === "string" || getClass(value) === "[object String]";
};

// is a given value undefined?
is.undefined = function(value) {
    return typeof value === "undefined";
};

// is a given value window?
// setInterval method is only available for window object
is.windowObject = function(value) {
    return value !== null && typeof value !== "undefined" &&
        typeof value === "object" && "setInterval" in value;
};

/////////////////////////////////////////////////////////////////////////////////
// Presence checks
/////////////////////////////////////////////////////////////////////////////////

// is a given value empty? Objects, arrays, strings
is.empty = function(value) {
    if (is.object(value)) {
        var length = countOwnProperties(value);
        if (length === 0 || (length === 1 && is.array(value)) ||
                (length === 2 && is.arguments(value))) {
            return true;
        }
        return false;
    }
    return value === "";
};

// is a given value existy?
is.existy = function(value) {
    return value !== null && typeof value !== "undefined";
};

// is a given value falsy?
is.falsy = function(value) {
    return !value;
};

// is a given value truthy?
is.truthy = not(is.falsy);

/////////////////////////////////////////////////////////////////////////////////
// Arithmetic checks
/////////////////////////////////////////////////////////////////////////////////

// is a given number above minimum parameter?
is.above = function(n, min) {
    return is.all.number(n, min) && n > min;
};
// above method does not support 'all' and 'any' interfaces
is.above.api = ["not"];

// is a given number decimal?
is.decimal = function(n) {
    return is.number(n) && n % 1 !== 0;
};

// are given values equal? supports numbers, strings, regexes, booleans
is.equal = function(value, other) {
    // check 0 and -0 equity with Infinity and -Infinity
    if (is.all.number(value, other)) {
        return value === other && 1 / value === 1 / other;
    }
    // check regexes as strings too
    if (is.all.string(value, other) || is.all.regexp(value, other)) {
        return "" + value === "" + other;
    }
    if (is.all["boolean"](value, other)) {
        return value === other;
    }
    return false;
};
// equal method does not support 'all' and 'any' interfaces
is.equal.api = ["not"];

// is a given number even?
is.even = function(n) {
    return is.number(n) && n % 2 === 0;
};

// is a given number finite? (no coercion, unlike the global isFinite)
is.finite = function(n) {
    return is.number(n);
};

// is a given number infinite?
is.infinite = function(n) {
    return n === Infinity || n === -Infinity;
};

// is a given number integer?
is.integer = function(n) {
    return is.number(n) && Math.floor(n) === n;
};

// is a given number negative?
is.negative = function(n) {
    return is.number(n) && n < 0;
};

// is a given number odd?
is.odd = function(n) {
    return is.number(n) && (n % 2 === 1 || n % 2 === -1);
};

// is a given number positive?
is.positive = function(n) {
    return is.number(n) && n > 0;
};

// is a given number above maximum parameter?
is.under = function(n, max) {
    return is.all.number(n, max) && n < max;
};
// under method does not support 'all' and 'any' interfaces
is.under.api = ["not"];

// is a given number within minimum and maximum parameters?
is.within = function(n, min, max) {
    return is.all.number(n, min, max) && n > min && n < max;
};
// within method does not support 'all' and 'any' interfaces
is.within.api = ["not"];

/////////////////////////////////////////////////////////////////////////////////
// Format checks
/////////////////////////////////////////////////////////////////////////////////

// A string of digits, with an optional sign and one decimal point.
is.numeric = function(value) {
    if (is.not.string(value) || value.length < 1 || value.length > MAX_REGEXP_VALUE_LENGTH) {
        return false;
    }
    return isNumericText(value);
};
is.numericString = is.numeric;

// Anchored patterns with no nested unbounded quantifier, applied to a bounded value.
var regexes = {
    affirmative: /^(?:1|t(?:rue)?|y(?:es)?|ok(?:ay)?)$/,
    alphaNumeric: /^[A-Za-z0-9]+$/,
    caPostalCode: /^(?!.*[DFIOQU])[A-VXY][0-9][A-Z]\s?[0-9][A-Z][0-9]$/,
    creditCard: /^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|6(?:011|5[0-9]{2})[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|(?:2131|1800|35[0-9]{3})[0-9]{11})$/,
    dateString: /^(1[0-2]|0?[1-9])([\/-])(3[01]|[12][0-9]|0?[1-9])(?:\2)(?:[0-9]{2})?[0-9]{2}$/,
    eppPhone: /^\+[0-9]{1,3}\.[0-9]{4,14}(?:x.+)?$/,
    hexadecimal: /^(?:0x)?[0-9a-fA-F]+$/,
    hexColor: /^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/,
    nanpPhone: /^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$/,
    socialSecurityNumber: /^(?!000|666)[0-8][0-9]{2}-?(?!00)[0-9]{2}-?(?!0000)[0-9]{4}$/,
    timeString: /^(2[0-3]|[01]?[0-9]):([0-5]?[0-9]):([0-5]?[0-9])$/,
    ukPostCode: /^[A-Z]{1,2}[0-9RCHNQ][0-9A-Z]?\s?[0-9][ABD-HJLNP-UW-Z]{2}$|^[A-Z]{2}-?[0-9]{4}$/,
    usZipCode: /^[0-9]{5}(?:-[0-9]{4})?$/
};

function regexpCheck(name, pattern) {
    is[name] = function(value) {
        var text = getCheckableText(value);
        if (text === null || text.length > MAX_REGEXP_VALUE_LENGTH) {
            return false;
        }
        return pattern.test(text);
    };
}

// create regexp checks methods from the 'regexes' object
for (var regexp in regexes) {
    if (hasOwnProperty.call(regexes, regexp)) {
        regexpCheck(regexp, regexes[regexp]);
    }
}

// is a given value a social security number? (is.js 0.9.0 name is is.socialSecurityNumber)
is.ssn = is.socialSecurityNumber;

// is a given value a URL? (parsed, not matched: see the note at the top of this file)
is.url = function(value) {
    return is.string(value) && isValidUrl(value);
};

// is a given value an email address? (parsed, not matched)
is.email = function(value) {
    return is.string(value) && isValidEmailAddress(value);
};

// is a given value an IPv4 address?
is.ipv4 = function(value) {
    return is.string(value) && isValidIpv4(value);
};

// is a given value an IPv6 address?
is.ipv6 = function(value) {
    return is.string(value) && isValidIpv6(value);
};

// simplify IP checks by calling the helpers for IPv4 and IPv6
is.ip = function(value) {
    return is.ipv4(value) || is.ipv6(value);
};

/////////////////////////////////////////////////////////////////////////////////
// String checks
/////////////////////////////////////////////////////////////////////////////////

// is a given string or sentence capitalized?
is.capitalized = function(string) {
    if (is.not.string(string)) {
        return false;
    }
    var words = string.split(" ");
    for (var i = 0; i < words.length; i++) {
        var word = words[i];
        if (word.length) {
            var chr = word.charAt(0);
            if (chr !== chr.toUpperCase()) {
                return false;
            }
        }
    }
    return true;
};

// is string end with a given target parameter?
is.endWith = function(string, target) {
    if (is.not.string(string)) {
        return false;
    }
    target += "";
    var position = string.length - target.length;
    return position >= 0 && string.indexOf(target, position) === position;
};
// endWith method does not support 'all' and 'any' interfaces
is.endWith.api = ["not"];

// is a given string include parameter target?
is.include = function(string, target) {
    return is.string(string) && string.indexOf(target) > -1;
};
// include method does not support 'all' and 'any' interfaces
is.include.api = ["not"];

// is a given string all lowercase?
is.lowerCase = function(string) {
    return is.string(string) && string === string.toLowerCase();
};

// is a given string palindrome?
is.palindrome = function(string) {
    if (is.not.string(string)) {
        return false;
    }
    string = string.replace(/[^a-zA-Z0-9]+/g, "").toLowerCase();
    var length = string.length - 1;
    for (var i = 0, half = Math.floor(length / 2); i <= half; i++) {
        if (string.charAt(i) !== string.charAt(length - i)) {
            return false;
        }
    }
    return true;
};

// is a given value space?
// horizontal tab: 9, line feed: 10, vertical tab: 11, form feed: 12, carriage return: 13, space: 32
is.space = function(value) {
    if (is.not["char"](value)) {
        return false;
    }
    var charCode = value.charCodeAt(0);
    return (charCode > 8 && charCode < 14) || charCode === 32;
};

// is string start with a given target parameter?
is.startWith = function(string, target) {
    return is.string(string) && string.indexOf(target) === 0;
};
// startWith method does not support 'all' and 'any' interfaces
is.startWith.api = ["not"];

// is a given string all uppercase?
is.upperCase = function(string) {
    return is.string(string) && string === string.toUpperCase();
};

/////////////////////////////////////////////////////////////////////////////////
// Time checks
/////////////////////////////////////////////////////////////////////////////////

var days = ["sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"];
var months = ["january", "february", "march", "april", "may", "june", "july", "august",
    "september", "october", "november", "december"];

// is a given dates day equal given day parameter?
is.day = function(date, day) {
    return is.date(date) && is.string(day) && day.toLowerCase() === days[date.getDay()];
};
// day method does not support 'all' and 'any' interfaces
is.day.api = ["not"];

// is a given date in daylight saving time?
is.dayLightSavingTime = function(date) {
    if (is.not.date(date)) {
        return false;
    }
    var january = new Date(date.getFullYear(), 0, 1);
    var july = new Date(date.getFullYear(), 6, 1);
    var stdTimezoneOffset = Math.max(january.getTimezoneOffset(), july.getTimezoneOffset());
    return date.getTimezoneOffset() < stdTimezoneOffset;
};

// is a given date future?
is.future = function(date) {
    var now = new Date();
    return is.date(date) && date.getTime() > now.getTime();
};

// is date within given range?
is.inDateRange = function(date, start, end) {
    if (is.not.date(date) || is.not.date(start) || is.not.date(end)) {
        return false;
    }
    var stamp = date.getTime();
    return stamp > start.getTime() && stamp < end.getTime();
};
// inDateRange method does not support 'all' and 'any' interfaces
is.inDateRange.api = ["not"];

// is a given date in last month range?
is.inLastMonth = function(date) {
    return is.inDateRange(date, new Date(new Date().setMonth(new Date().getMonth() - 1)), new Date());
};

// is a given date in last week range?
is.inLastWeek = function(date) {
    return is.inDateRange(date, new Date(new Date().setDate(new Date().getDate() - 7)), new Date());
};

// is a given date in last year range?
is.inLastYear = function(date) {
    return is.inDateRange(date, new Date(new Date().setFullYear(new Date().getFullYear() - 1)), new Date());
};

// is a given date in next month range?
is.inNextMonth = function(date) {
    return is.inDateRange(date, new Date(), new Date(new Date().setMonth(new Date().getMonth() + 1)));
};

// is a given date in next week range?
is.inNextWeek = function(date) {
    return is.inDateRange(date, new Date(), new Date(new Date().setDate(new Date().getDate() + 7)));
};

// is a given date in next year range?
is.inNextYear = function(date) {
    return is.inDateRange(date, new Date(), new Date(new Date().setFullYear(new Date().getFullYear() + 1)));
};

// is the given year a leap year?
is.leapYear = function(year) {
    return is.number(year) && ((year % 4 === 0 && year % 100 !== 0) || year % 400 === 0);
};

// is a given dates month equal given month parameter?
is.month = function(date, month) {
    return is.date(date) && is.string(month) && month.toLowerCase() === months[date.getMonth()];
};
// month method does not support 'all' and 'any' interfaces
is.month.api = ["not"];

// is a given date past?
is.past = function(date) {
    var now = new Date();
    return is.date(date) && date.getTime() < now.getTime();
};

// is a given date in the parameter quarter?
is.quarterOfYear = function(date, quarter) {
    return is.date(date) && is.number(quarter) && quarter === Math.floor((date.getMonth() + 3) / 3);
};
// quarterOfYear method does not support 'all' and 'any' interfaces
is.quarterOfYear.api = ["not"];

// is a given date indicate today?
is.today = function(date) {
    var now = new Date();
    var todayString = now.toDateString();
    return is.date(date) && date.toDateString() === todayString;
};

// is a given date indicate tomorrow?
is.tomorrow = function(date) {
    var now = new Date();
    var tomorrowString = new Date(now.setDate(now.getDate() + 1)).toDateString();
    return is.date(date) && date.toDateString() === tomorrowString;
};

// is a given date weekend? (6: Saturday, 0: Sunday)
is.weekend = function(date) {
    return is.date(date) && (date.getDay() === 6 || date.getDay() === 0);
};

// is a given date weekday? A value that is not a date is not a weekday either (is.js
// 0.9.0 built this as not(is.weekend), so is.weekday(null) was true).
is.weekday = function(date) {
    return is.date(date) && date.getDay() !== 6 && date.getDay() !== 0;
};

// is a given dates year equal given year parameter?
is.year = function(date, year) {
    return is.date(date) && is.number(year) && year === date.getFullYear();
};
// year method does not support 'all' and 'any' interfaces
is.year.api = ["not"];

// is a given date indicate yesterday?
is.yesterday = function(date) {
    var now = new Date();
    var yesterdayString = new Date(now.setDate(now.getDate() - 1)).toDateString();
    return is.date(date) && date.toDateString() === yesterdayString;
};

/////////////////////////////////////////////////////////////////////////////////
// Environment checks
/////////////////////////////////////////////////////////////////////////////////

// is the script running on Windows?
is.windows = function() {
    return /win/.test(getHostPlatform());
};
// windows method does not support 'all' and 'any' interfaces
is.windows.api = ["not"];

// is the script running on Linux?
is.linux = function() {
    var platform = getHostPlatform();
    return /linux/.test(platform) && !/android/.test(platform);
};
// linux method does not support 'all' and 'any' interfaces
is.linux.api = ["not"];

// is the script running on macOS?
is.mac = function() {
    return /mac/.test(getHostPlatform());
};
// mac method does not support 'all' and 'any' interfaces
is.mac.api = ["not"];

// is the current state online? In a browser host this is navigator.onLine. In Windows
// Script Host, which has no navigator, the host counts as online when an enabled network
// adapter has a default gateway (Win32_NetworkAdapterConfiguration), the same kind of
// answer navigator.onLine gives: a network is connected, not that a given server can be
// reached. When neither can be read (no WMI in this host), the answer is true, as before.
is.online = function() {
    try {
        if (typeof navigator !== "undefined" && navigator && typeof navigator.onLine === "boolean") {
            return navigator.onLine;
        }
    } catch (e) {
        // No navigator object in this host.
    }

    try {
        var service = GetObject("winmgmts:{impersonationLevel=impersonate}!\\\\.\\root\\cimv2");
        var adapters = new Enumerator(service.ExecQuery(
            "SELECT DefaultIPGateway FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE"));
        for (; !adapters.atEnd(); adapters.moveNext()) {
            var gateways = adapters.item().DefaultIPGateway;
            if (gateways !== null && typeof gateways !== "undefined" &&
                    new VBArray(gateways).toArray().length > 0) {
                return true;
            }
        }
        return false;
    } catch (e) {
        return true;
    }
};
// online method does not support 'all' and 'any' interfaces
is.online.api = ["not"];

// is the current state offline?
is.offline = not(is.online);
// offline method does not support 'all' and 'any' interfaces
is.offline.api = ["not"];

/////////////////////////////////////////////////////////////////////////////////
// Object checks
/////////////////////////////////////////////////////////////////////////////////

// has a given object got parameterized count property?
is.propertyCount = function(object, count) {
    if (is.not.object(object) || is.not.number(count)) {
        return false;
    }
    var n = 0;
    for (var property in object) {
        if (hasOwnProperty.call(object, property) && ++n > count) {
            return false;
        }
    }
    return n === count;
};
// propertyCount method does not support 'all' and 'any' interfaces
is.propertyCount.api = ["not"];

// is given object has parameterized property?
is.propertyDefined = function(object, property) {
    return is.object(object) && is.string(property) && property in object;
};
// propertyDefined method does not support 'all' and 'any' interfaces
is.propertyDefined.api = ["not"];

// is a given value thenable (like Promise)?
is.thenable = function(value) {
    return is.object(value) && typeof value.then === "function";
};

/////////////////////////////////////////////////////////////////////////////////
// Array checks
/////////////////////////////////////////////////////////////////////////////////

// is a given item in an array?
is.inArray = function(value, array) {
    if (is.not.array(array)) {
        return false;
    }
    for (var i = 0; i < array.length; i++) {
        if (array[i] === value) {
            return true;
        }
    }
    return false;
};
// inArray method does not support 'all' and 'any' interfaces
is.inArray.api = ["not"];

// is a given array sorted?
is.sorted = function(array, sign) {
    if (is.not.array(array)) {
        return false;
    }
    var predicate = comparator[sign] || comparator[">="];
    for (var i = 1; i < array.length; i++) {
        if (!predicate(array[i], array[i - 1])) {
            return false;
        }
    }
    return true;
};

// is.fn is the name the is.js documentation uses for is["function"]
is.fn = is["function"];

/////////////////////////////////////////////////////////////////////////////////
// Interfaces (is.not, is.all, is.any)
/////////////////////////////////////////////////////////////////////////////////

// Set 'not', 'all' and 'any' interfaces to methods based on their api property
function setInterfaces() {
    for (var option in is) {
        if (hasOwnProperty.call(is, option) && is["function"](is[option])) {
            var interfaces = is[option].api || ["not", "all", "any"];
            for (var i = 0; i < interfaces.length; i++) {
                if (interfaces[i] === "not") {
                    is.not[option] = not(is[option]);
                }
                if (interfaces[i] === "all") {
                    is.all[option] = all(is[option]);
                }
                if (interfaces[i] === "any") {
                    is.any[option] = any(is[option]);
                }
            }
        }
    }
}
setInterfaces();

/////////////////////////////////////////////////////////////////////////////////
// Configuration methods
/////////////////////////////////////////////////////////////////////////////////

// Set the pattern of a check, or add a check of its own.
//   is.setRegexp(/^[0-9]{6}$/, "postalCode");
is.setRegexp = function(regexp, name) {
    is[name] = function(value) {
        var text = getCheckableText(value);
        if (text === null) {
            return false;
        }
        return regexp.test(text);
    };
};

// Put back the global "is" that was there before this module was loaded, and return this
// object instead, so that a page with its own "is" keeps it (is.js 0.9.0 did the same).
is.setNamespace = function() {
    if (typeof global !== "undefined" && global) {
        global.is = previousGlobalIs;
    }
    return is;
};

var previousGlobalIs = (typeof global !== "undefined" && global) ? global.is : undefined;

// app.js assigns the module to the top level variable "is" as well; publishing it here
// keeps the global available to a script that requires the module on its own.
if (typeof global !== "undefined" && global) {
    global.is = is;
}

is.VERSIONINFO = "WelsonJS is.js-compatible checks (is.js) version " + is.VERSION;
is.AUTHOR = "gnh1201@catswords.re.kr";
is.global = global;
is.require = global.require;

module.exports = is;
