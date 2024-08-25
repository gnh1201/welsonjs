function main(args) {
    console.log("WelsonJS.Service required.");
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

function onServiceMessageReceived(args) {
    return "onMessageReceived recevied. " + args.join(', ');
}

function onServiceScreenTime(args) {
    return "onServiceScreenTime recevied. " + args.join(', ');
}

function onServiceFileCreated(args) {
    return "onFileCreated recevied. " + args.join(', ');
}

function onServiceFileRuleMatched(args) {
    return "onFileCreated recevied. " + args.join(', ');
}

exports.main = main;
exports.getDeviceID = getDeviceID;
exports.onServiceStart = onServiceStart;
exports.onServiceStop = onServiceStop;
exports.onServiceElapsedTime = onServiceElapsedTime;
exports.onServiceScreenTime = onServiceScreenTime;
exports.onFileCreated = onFileCreated;
exports.onFileRuleMatched = onFileRuleMatched;
