// virustotal.js
// Namhyeon Go <abuse@catswords.net>

var FILE = require("lib/file");
var HTTP = require("lib/http");
var Chrome = require("lib/chrome");
var RAND = require("lib/rand");

function getHashes() {
    var lines = [];

    var rows = splitLn(FILE.readFile("data\\hashes.txt", "utf-8"));
    for (var i = 0; i < rows.length; i++) {
        var row = rows[i].split(',');
        if (row.length == 2) lines.push(row);
    }

    return lines;
}

function main(args) {
    var hashes = getHashes();
    var lines = [];
    var wbInstance = Chrome.startWithDebugging("https://virustotal.com", null, "virustotal", 9222);

    // 대기
    sleep(5000);

    // 브라우저 탭 찾기
    pages = wbInstance.getPagesByTitle("VirusTotal");
    if (pages.length > 0) {
        page = pages[0];
        wbInstance.setPageId(page.id);
        wbInstance.autoAdjustByWindow(1920, 1080, 1050, 1200, 800, 950);
    }

    var callback1 = function(row) {
        var hash = row[1];

        console.log("탐색을 시도합니다:", hash);

        wbInstance.navigate("https://www.virustotal.com/gui/file/" + hash);
        sleep(RAND.getInt(4000, 5000));

        // 자료를 찾았는지 여부
        var msgNotFound = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "search-view",
                "vt-ui-special-states"
            ]) +
            ".shadowRoot.querySelector('.title slot').assignedNodes()[0].innerText"
        );
        if (msgNotFound == "No matches found") {
            console.log("찾을 수 없음 (No matches found): " + hash);
            lines.push([hash, '', '0', '0', '0', ''].join(','));
            return;
        }

        // 전체 진단 수 확인
        var positives = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "file-view",
                "vt-ui-main-generic-report",
                "vt-ui-detections-widget"
            ]) +
            ".shadowRoot.querySelector('div > div > div.positives').innerText"
        );

        // 알려진 파일 명
        var filename = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "file-view",
                "vt-ui-file-card"
            ]) +
            ".shadowRoot.querySelector('vt-ui-generic-card > div:nth-child(2) > div:nth-child(1) > div.object-id > div.file-name > a').innerText"
        );

        // 최근 진단 날짜 확인
        var last = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "file-view",
                "vt-ui-file-card"
            ]) +
            ".shadowRoot.querySelector('vt-ui-generic-card > div:nth-child(2) > div:nth-child(1) > div:nth-child(3) > div').innerText"
        );

        // 국내 백신 진단 여부 확인
        var score_undetected = wbInstance.getEvaluatedValue(
            'Object.values(' +
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2].querySelector('file-view').shadowRoot.querySelector('vt-ui-main-generic-report').querySelector('.tab-slot')" +
            wbInstance.getShadowRootSelector([
                "vt-ui-detections-list",
                "vt-ui-expandable"
            ]) +
            ".querySelectorAll('.detection')).reduce(function(a, x) { if(/AhnLab|ALYac|ViRobot/.test(x.innerText) && x.innerText.indexOf('Undetected') > -1) a = a + 1; return a; }, 0)"
        );

        console.log("해시:", hash);
        console.log("알려진 파일 이름:", filename);
        console.log("전체 진단:", positives);
        console.log("최근 날짜:", last);
        console.log("국내 백신 미진단:", score_undetected + "건");

        // 쓰기 줄 생성
        lines.push([hash, filename, '1', positives, score_undetected, last].join(','));
    };

    hashes.forEach(callback1);

    FILE.appendFile("data\\vt_matches_2.txt", lines.join("\r\n"), "utf-8");
}

exports.main = main;
