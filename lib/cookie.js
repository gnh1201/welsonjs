// cookie.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var CookieObject = function() {
    this.expireDays = 90;

    this.setExpireDays = function(days) {
        this.expireDays = days;
        return this;
    };

    this.isEnabled = function() {
        return (typeof(document) !== "undefined" && typeof(document.cookie) !== "undefined");
    };

    this.add = function(key, value) {
        if (this.isEnabled()) {
            var todayDate = new Date();
            todayDate.setDate(todayDate.getDate() + expiredays);
            document.cookie = key + "=" + escape(value) + "; path=/; expires=" + todayDate.toGMTString() + ";"
        } else {
            console.warn("CookieObject.add() -> Cookie does not supported on this environment");
        }
    };

    this.get = function(key) {
        if (this.isEnabled()) {
            var result = null;
            var cookie = document.cookie.split(';');
            cookie.some(function (item) {
                // 공백을 제거
                item = item.replace(' ', '');
         
                var dic = item.split('=');
         
                if (key === dic[0]) {
                    result = dic[1];
                    return true;    // break;
                }
            });
            return result;
        } else {
            console.warn("CookieObject.add() -> Cookie does not supported on this environment");
        }
    };
};