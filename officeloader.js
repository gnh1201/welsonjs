// officeloader.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var SYS = require("lib/system");
var Office = require("lib/msoffice");
var ChatGPT = require("lib/chatgpt");

function main(args) {
    // EXAMPLE: cscript app.js officeloader <data\example.xlsx> <programfile>
    // TEST: cscript app.js testloader open_excel_file data\test-msoffice-20231219.json
    if (args.length > 0) {
        var filename = args[0];
        var programfile = args[1];
        open(filename, programfile);
    } else {
        console.error("Insufficient arguments");
    }
}

function open(filename, programfile) {
    var filetypes = [
        {"application": "excel", "filetypes": Office.Excel.SupportedFileTypes},
        {"application": "powerpoint", "filetypes": Office.PowerPoint.SupportedFileTypes},
        {"application": "word", "filetypes": Office.Word.SupportedFileTypes}
    ];

    var resolved_application = filetypes.reduce(function(a, x) {
        if (a == '') {
            var application = x.application;
            var extensions = x.filetypes.reduce(function(b, x) {
                return b.concat(x.extension);
            }, []);

            return extensions.reduce(function(b, x) {
                return (b == '' && filename.lastIndexOf(x) > -1 ? application : b);
            }, '');
        }

        return a;
    }, '');

    var after_opened = function(officeInstance) {
        if (typeof programfile !== "undefined") {
            var target = require(programfile);
            try {
                target.onApplicationOpened(resolved_application, officeInstance);
            } catch (e) {
                console.error("after_opened:", e.message);
            }
        };
    };

    switch (resolved_application) {
        case "excel": {
            var excel = new Office.Excel();   // Create an Excel instance
            excel.open(filename);  // Open the Excel instance
            after_opened(excel);
            break;
        }
        
        case "powerpoint": {
            var powerpoint = new Office.PowerPoint();   // Create a PowerPoint instance
            powerpoint.open(filename);  // Open the PowerPoint instance
            after_opened(powerpoint);
            break;
        }

        case "word": {
            var word = new Office.Word();   // Create an Word instance
            word.open(filename);  // Open the Word instance
            after_opened(word);
            break;
        }

        default: {
            console.error("Not supported filetype");
        }
    }
}

exports.main = main;
