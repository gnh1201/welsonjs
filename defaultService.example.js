// defaultService.example.js
// https://github.com/gnh1201/welsonjs
var SYS = require("lib/system");

function main(args) {
    console.log("WelsonJS.Service required.");
}

function getDeviceID() {
    return SYS.getUUID();
}

function onServiceStart(args) {
    return "onServiceStart recevied. " + args.join(', ');
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

function onScreenNextTemplate(args) {
    return "example.png";
}

function onScreenTemplateMatched(args) {
    return "onScreenTemplateMatched recevied. " + args.join(', ');
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

function onAvScanResult(args) {
    return "onAvScanResult recevied. " + args.join(', ');
}

exports.main = main;
exports.getDeviceID = getDeviceID;
exports.onServiceStart = onServiceStart;
exports.onServiceStop = onServiceStop;
exports.onServiceElapsedTime = onServiceElapsedTime;
exports.onScreenNextTemplate = onScreenNextTemplate;
exports.onScreenTemplateMatched = onScreenTemplateMatched;
exports.onFileCreated = onFileCreated;
exports.onNetworkConnected = onNetworkConnected;
exports.onRegistryModified = onRegistryModified;
exports.onAvScanResult = onAvScanResult;
