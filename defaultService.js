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

function onServiceScreenTime(args) {
    return "onServiceScreenTime recevied. " + args.join(', ');
}

function onFileCreated(args) {
    return "onFileCreated recevied. " + args.join(', ');
}

function onFileRuleMatched(args) {
    return "onFileCreated recevied. " + args.join(', ');
}

exports.main = main;
exports.onServiceStart = onServiceStart;
exports.onServiceStop = onServiceStop;
exports.onServiceElapsedTime = onServiceElapsedTime;
exports.onServiceScreenTime = onServiceScreenTime;
exports.onFileCreated = onFileCreated;
exports.onFileRuleMatched = onFileRuleMatched;
