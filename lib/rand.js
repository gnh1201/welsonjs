function getInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
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
        result += characters.charAt(Math.floor(Math.random() * charactersLength));
    }

    return result;
}

exports.getInt = getInt;
exports.one = one;
exports.makeString = makeString;

exports.VERSIONINFO = "Random Module (random.js) version 0.1";
exports.global = global;
exports.require = global.require;
