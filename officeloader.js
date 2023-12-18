// officeloader.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
var SYS = require("lib/system");
var Office = require("lib/msoffice");
var ChatGPT = require("lib/chatgpt");

function main(args) {
    // EXAMPLE: cscript app.js officeloader <data\example.xlsx> <programfile>
    if (args.length > 0) {
        var filename = args[0];
        var programfile = args[1];
        open(filename, programfile);
    } else {
        test();
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

function test() {
    // 엑셀 인스턴스 생성
    var excel = new Office.Excel();

    // 질문 목록
    var questions = [
        "엄마가 좋아 아빠가 좋아?",
        "나도 모르는 사이 내 통장에 100억이 입금되어 있다면?",
        "내 친구가 술에 취해 뻗었는데 신민아가 단둘이 술 먹자고 한다면, 친구 버리고 가도 되나?",
        "회사 야유회를 가서 과장님과 같은 텐트에서 자고 있는데 우리 텐트에 뱀이 들어왔다면 과장님 깨워야 되나, 안 깨우고 혼자 도망쳐야 되나?"
    ];

    // 엑셀 열기
    excel.open();
    
    // 질문에 답하기
    var i = 1;
    questions.forEach(function(x) {
        var answer = ChatGPT.chat(x);
        console.log("받은 답변:", answer);
        excel.getCellByPosition(i, 1).setValue(answer);
        i++;
    });

    // 엑셀 닫기
    //excel.close();
}

exports.main = main;
