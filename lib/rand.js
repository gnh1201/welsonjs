// rand.js

var FILE = require("lib/file");

function randomize() {
    return Math.random();
}

function getInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(randomize() * (max - min + 1)) + min;
}

function getSeed() {
    return getInt(1000000000, 9999999999);
}

function one(arr) {
    return arr[getInt(0, arr.length - 1)];
}

function makeString(length, characterNames) {
    var result           = '';
    var characters       = '';
    
    for (var i = 0; i < characterNames.length; i++) {
        switch (characterNames[i]) {
            case 'uppercase':
                characters += 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
                break;
                
            case 'lowercase':
                characters += 'abcdefghijklmnopqrstuvwxyz';
                break;

            case 'number':
                characters += '0123456789';
                break;
            
            case 'base64':
                characters += 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
                break;

            case 'hex':
                characters += '0123456789abcdef';
                break;

            default:
                characters += characterNames[i];
        }
    }

    var charactersLength = characters.length;
    for ( var i = 0; i < length; i++ ) {
        result += characters.charAt(Math.floor(randomize() * charactersLength));
    }

    return result;
}

// UUID v4
function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = randomize() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function shuffle(arr) {
    return arr.sort(function() {
        return randomize() - 0.5;
    });
}

function rotate(arr, i) {
    return arr[i % arr.length];
}

function sample(arr, length) {
    return shuffle(arr).slice(0, length);
}

function discardCallback(callback, args, filename) {
    if (typeof callback !== "function") return;

    var discarded = splitLn(FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8));
    var chosen = callback.apply(null, args);

    while (chosen == null || discarded.indexOf(chosen) > -1) {
        console.log("다른 항목을 찾습니다.");
        chosen = callback.apply(null, args);
    }

    FILE.appendFile(filename, "\r\n" + chosen, FILE.CdoCharset.CdoUTF_8);

    return chosen;
}

function discardOne(arr, filename) {
    var discarded = splitLn(FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8));
    var _arr = arr.reduce(function(a, x) {
        if (discarded.indexOf(x) < 0) a.push(x);
        return a;
    }, []);

    return discardCallback(one, [_arr], filename);
}

exports.randomize = randomize;
exports.getInt = getInt;
exports.getSeed = getSeed;
exports.one = one;
exports.makeString = makeString;
exports.uuidv4 = uuidv4;
exports.shuffle = shuffle;
exports.rotate = rotate;
exports.sample = sample;
exports.discardOne = discardOne;
exports.discardCallback = discardCallback;

exports.VERSIONINFO = "Random Module (rand.js) version 0.6.1";
exports.global = global;
exports.require = global.require;
