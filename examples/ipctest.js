var PipeIPC = require("lib/pipe-ipc");
var RAND = require("lib/rand");

var texts = [
	"Think like a man of action and act like man of thought.",
	"Courage is very important. Like a muscle, it is strengthened by use.",
	"Life is the art of drawing sufficient conclusions from insufficient premises.",
	"By doubting we come at the truth.",
	"A man that hath no virtue in himself, ever envieth virtue in others.",
	"When money speaks, the truth keeps silent."
];

function main(args) {
	var pipe = PipeIPC.create("helloworld");
	var sender = RAND.uuidv4();
	
	while (true) {
		var text_out = RAND.one(texts);
		pipe.write("<" + sender + "> " + text_out);
		console.log("Sent: " + text_out);

		var text_in = pipe.read();
		while (text_in == ("<" + sender + "> " + text_out) || text_in == "") {
			sleep(1);
			text_in = pipe.read();
		}

		console.log("Recieved: " + text_in);
	}
};

exports.main = main;
