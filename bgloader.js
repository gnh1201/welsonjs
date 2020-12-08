function main() {
	var args = [];
	var argl = WScript.arguments.length;
	for (var i = 0; i < argl; i++) {
		args.push(WScript.arguments(i));
	}

	var objShell = WScript.CreateObject("WScript.Shell");
	objShell.Run(args.join(' '), 0, true);
}

main();