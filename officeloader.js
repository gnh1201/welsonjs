// officeloader.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

var SYS = require("lib/system");
var Office = require("lib/msoffice");
var ChatGPT = require("lib/chatgpt");

function main(args) {
    // 기존 파일을 여는 경우 인자에 파일 경로 추가
    if (args.length > 0) {
        var filename = args[0];
        open(filename);
    } else {
        test();
    }
}

function open(filename) {
    // 엑셀 인스턴스 생성
    var excel = new Office.Excel();
    
    // 엑셀 열기
    excel.open(filename);
    
    // .... 여기서 작업하세요 ....

    // 엑셀 닫기
    //excel.close();
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
        excel.setValueByPosition(i, 1, answer);
        i++;
    });

    // 엑셀 닫기
    //excel.close();
}

exports.main = main;
