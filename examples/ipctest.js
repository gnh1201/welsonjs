// a mafia style PIPE IPC based file I/O test

var FILE = require("lib/file");
var filename = "data\\dead_targets.txt";

function recordDead(name) {
    FILE.rotateFile(filename, name + "\r\n", 1000, "utf-8");
}

function checkIsDead(name) {
    var text = FILE.readFile(filename, "utf-8");
    var deadNames = splitLn(text);
    return deadNames.indexOf(name) > -1;
}

function main(args) {
    while (true)  {
        recordDead("kim@example.org");
        //recordDead("lee@example.org");
        recordDead("park@example.org");
        //recordDead("choi@example.org");
        recordDead("hong@example.org");

        console.log(checkIsDead("kim@example.org") ? "DEAD" : "ALIVE");
        console.log(checkIsDead("lee@example.org") ? "DEAD" : "ALIVE");
        console.log(checkIsDead("park@example.org") ? "DEAD" : "ALIVE");
        console.log(checkIsDead("choi@example.org") ? "DEAD" : "ALIVE");
        console.log(checkIsDead("hong@example.org") ? "DEAD" : "ALIVE");
    }
}

exports.main = main;
