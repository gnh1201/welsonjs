// config.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var XML = require("lib/xml");

exports.getValue = function(key) {
    return XML.load("config.xml").select("/Config/" + key).first().getText();
};
