// virustotal.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var FILE = require("lib/file");
var HTTP = require("lib/http");
var Chrome = require("lib/chrome");
var RAND = require("lib/rand");

function getHashes() {
    var rows = [];

    var lines = splitLn(FILE.readFile("data\\hashes.txt", FILE.CdoCharset.CdoUTF_8));
    for (var i = 0; i < lines.length; i++) {
        var row = lines[i].split(',');
        if (row.length == 2) rows.push(row);
        if (row.length == 1) rows.push(['', row[0]]);
    }

    return rows;
}

function main(args) {
    var hashes = getHashes();
    var lines = [];
    var wbInstance = Chrome.startWithDebugging("https://virustotal.com", null, "virustotal", 9222);

    // Wait
    sleep(5000);

    // finding browser tap
    pages = wbInstance.getPagesByTitle("VirusTotal");
    if (pages.length > 0) {
        page = pages[0];
        wbInstance.setPageId(page.id);
        wbInstance.autoAdjustByWindow(1920, 1080, 1050, 1200, 800, 950);
    }

    var callback1 = function(row) {
        var hash = row[1];

        console.log("Attempt exploring:", hash);

        wbInstance.navigate("https://www.virustotal.com/gui/file/" + hash);
        sleep(RAND.getInt(4000, 5000));

        // Indicates whether data was found
        var msgNotFound = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "search-view",
                "vt-ui-special-states"
            ]) +
            ".shadowRoot.querySelector('.title slot').assignedNodes()[0].innerText"
        );
        if (msgNotFound == "No matches found") {
            console.log("No matches found"): " + hash);
            lines.push([hash, '', '0', '0', '0', ''].join(','));
            return;
        }

        // Check how many times it was examined to detect and diagnose viruses
        var positives = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "file-view",
                "vt-ui-main-generic-report",
                "vt-ui-detections-widget"
            ]) +
            ".shadowRoot.querySelector('div > div > div.positives').innerText"
        );

        // known filename
        var filename = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "file-view",
                "vt-ui-file-card"
            ]) +
            ".shadowRoot.querySelector('div > div.card-body > div > div.hstack.gap-4 > div.vstack.gap-2.align-self-center.text-truncate > div.file-name.text-truncate > a').innerText"
        );

        // Check the latest date of the examination to detect and diagnose a virus
        var last = wbInstance.getEvaluatedValue(
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2]" +
            wbInstance.getShadowRootSelector([
                "file-view",
                "vt-ui-file-card"
            ]) +
            ".shadowRoot.querySelector('div > div.card-body > div > div.hstack.gap-4 > div:nth-child(5) > vt-ui-time-ago').getAttribute('data-tooltip-text')"
        );

        // Check whether a Korean vaccine programme has examined to detect and diagnose virus
        var score_undetected = wbInstance.getEvaluatedValue(
            'Object.values(' +
            "__getDocument().querySelector('vt-ui-shell').shadowRoot.querySelector('#mainContent').querySelector('slot').assignedNodes()[2].querySelector('file-view').shadowRoot.querySelector('vt-ui-main-generic-report').querySelector('.tab-slot')" +
            wbInstance.getShadowRootSelector([
                "vt-ui-detections-list",
                "vt-ui-expandable"
            ]) +
            ".querySelectorAll('.detection')).reduce(function(a, x) { if(/AhnLab|ALYac|ViRobot/.test(x.innerText) && x.innerText.indexOf('Undetected') > -1) a = a + 1; return a; }, 0)"
        );

        console.log("hash:", hash);
        console.log("known filename:", filename);
        console.log("examined the whole to detect and diagnose viruses :", positives);
        console.log("date of the latest activity:", last);
        console.log("Hasn ' t examined with a Korean vaccine programme to detect and diagnose viruses:", score_undetected + "건");

        // create lines for writing
        lines.push([hash, filename, '1', positives, score_undetected, last].join(','));
    };

    hashes.forEach(callback1);

    FILE.appendFile("data\\vt_matches_2.txt", lines.join("\r\n"), FILE.CdoCharset.CdoUTF_8);
}

exports.main = main;
