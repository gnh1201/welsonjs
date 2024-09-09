var SYS = require("lib/system");

function main(args) {
    console.log("WelsonJS.Service required.");
}

function getDeviceID() {
    return SYS.getUUID();
}

function onServiceStart() {
    return "onServiceStart recevied";
}

function onServiceStop() {
    return "onServiceStop recevied";
}

function onServiceElapsedTime() {
    return "onServiceElapsedTime recevied";
}

function onMessageReceived() {
    return "onMessageReceived recevied. " + args.join(', ');
}

function onServiceScreenTime(args) {
    return "onServiceScreenTime recevied. " + args.join(', ');
}

function onFileCreated(args) {
    return "onFileCreated recevied. " + args.join(', ');
}

function onNetworkConnected(args) {
    return "onNetworkConnected recevied. " + args.join(', ');
}

function onRegistryModified(args) {
    return "onRegistryModified recevied. " + args.join(', ');
}


exports.main = main;
exports.getDeviceID = getDeviceID;
exports.onServiceStart = onServiceStart;
exports.onServiceStop = onServiceStop;
exports.onServiceElapsedTime = onServiceElapsedTime;
exports.onServiceScreenTime = onServiceScreenTime;
exports.onFileCreated = onFileCreated;
exports.onNetworkConnected = onNetworkConnected;
exports.onRegistryModified = onRegistryModified;
