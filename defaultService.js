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

function onServiceScreenTime(filename, screenNumber, x, y, maxCorrelation) {
    return "onServiceScreenTime recevied (" + [filename, screenNumber, x, y, maxCorrelation].join(',') + ")";
}

exports.main = main;
exports.onServiceStart = onServiceStart;
exports.onServiceStop = onServiceStop;
exports.onServiceElapsedTime = onServiceElapsedTime;
exports.onServiceScreenTime = onServiceScreenTime;
