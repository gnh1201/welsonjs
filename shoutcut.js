var Chrome = require("lib/chrome");

var do_Chrome = function(args) {
	Chrome.create().open("https://google.com");
};

exports.main = function(args) {
    if (args.length < 1) {
        console.error("arguments could not empty.")
        return;
    }

    while (true) {
        sleep(1000);
        switch (args[0]) {
            case "chrome":
                return do_Chrome(args);
        }
    }
};
