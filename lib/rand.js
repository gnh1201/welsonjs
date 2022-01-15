// rand.js

function randomize() {
    return Math.random();
}

function getInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(randomize() * (max - min + 1)) + min;
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
};

exports.randomize = randomize;
exports.getInt = getInt;
exports.one = one;
exports.makeString = makeString;
exports.uuidv4 = uuidv4;
exports.shuffle = shuffle;
exports.rotate = rotate;

exports.VERSIONINFO = "Random Module (random.js) version 0.2";
exports.global = global;
exports.require = global.require;
