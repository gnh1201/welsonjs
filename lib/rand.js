function getInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function one(arr) {
	return arr[getInt(0, arr.length - 1)];
}

exports.getInt = getInt;
exports.one = one;

exports.VERSIONINFO = "Random Module (random.js) version 0.1";
exports.global = global;
exports.require = global.require;
