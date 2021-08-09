function main(args) {
	var toolkit = CreateObject("WelsonJS.Toolkit");

	console.log(toolkit.SendClick("gnh1201", 30, 30));
}

exports.main = main;