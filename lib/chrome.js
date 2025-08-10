// chrome.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Chrome Web Browser Debugging Interface for WelsonJS framework
// 
// To use this feature, a gateway that converts between stateless HTTP and WebSocket protocols is required.
// Run the WelsonJS Launcher, which supports this conversion, or use another compatible tool.
// 
var STD = require("lib/std");
var RAND = require("lib/rand");
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");
var HTTP = require("lib/http");
var AutoIt = require("lib/autoit");
var Toolkit = require("lib/toolkit");
var ExtraMath = require("lib/extramath");

// for Chromium-based browsers
var pageEventId = new STD.Accessor(0);
var publisherName = new STD.Accessor("chrome");

var ChromeObject = function() {
    STD.EventTarget.apply(this, arguments);  // Set event-attachable object

    this.workingDirectory = null;
    this.binPath = null;

    this.profileName = "Default";
    this.userDataDir = null;  // Default set to null after 0.2.7.54; change via .setUserDataDir.
    this.defaultUserDataDir = null;  // Set by .setPublisherName; not frequently referenced.
    this.installedDir = "Chrome";

    // proxy
    this.isPreventProxy = false;
    this.proxy = {
        "protocol": "socks5",
        "host": "127.0.0.1",
        "port": 1080,
        "userAgent": null
    };
    this.inPrivate = false;

    // user agent
    this.userAgent = null;
    this.userAgents = [];

    // dependencies
    this.oAutoIt = null;

    // for remote debugging
    this.debuggingPort = 0;
    this.pageId = "";
    this.ws = Websocket.create();
    this.isAttached = false;
    this.pageList = [];
    this.title = "";
    this.frameIndex = -1;
    this.disableFeatures = ["ShowBookmarksBar", "Translate"];

    this.isPreventEvaluate = false;
    this.isAppMode = false;
    this.baseScreenX = 0;
    this.baseScreenY = 0;

    this.create = function() {
        this.oAutoIt = AutoIt.create();
        this.baseScreenX = 1;
        this.baseScreenY = (!this.isAppMode ? 84 : 32);
        this.setPublisherName(publisherName.get());
        
        return this;
    };

    this.setIsPreventEvaluate = function(flag) {
        this.isPreventEvaluate = flag;
    };

    this.setBinPath = function(path) {
        this.binPath = path;
        return this;
    };

    this.setProfile = function(profileName, installedDir) {
        this.profileName = (profileName == "Default" ? "Chrome" : profileName);
        this.setInstalledDir(installedDir);
        this.workingDirectory = this.workingDirectory.replace(":installedDir", this.installedDir);
        this.binPath = this.binPath.replace(":installedDir", this.installedDir);
        return this;
    };

    this.clearProfile = function() {
        var FN = this.userDataDir;
        while (FILE.folderExists(FN)) {
            try {
                return FILE.deleteFolder(FN);
            } catch (e) {
                console.warn("Can not clear the session! " + e.message);
                console.warn("Retrying clear the profile...");
            }
        }
    };

    this.clear = function() {
        return this.clearProfile();
    };

    this.setUserDataDir = function(dirname) {
        if (dirname != null) {
            this.userDataDir = dirname;
        } else {
            this.userDataDir = SYS.getEnvString("APPDATA") + "\\WelsonJS\\" + publisherName.get() + "_user_profile";
        }
        return this;
    };
    
    this.setInstalledDir = function(dirname) {
        if (dirname != null) {
            this.installedDir = dirname;
        }
        return this;
    };
    
    this.setProxy = function(obj) {
        this.proxy = obj;
        return this;
    };

    this.setProxyProtocol = function(s) {
        this.proxy.protocol = s;
        return this;
    };

    this.setProxyHost = function(s) {
        this.proxy.host = s;
        return this;
    };

    this.setProxyPort = function(port) {
        this.proxy.port = port;
        return this;
    };

    this.setIsPreventProxy = function(flag) {
        this.isPreventProxy = flag;
        return this;
    };

    this.setDebuggingPort = function(port) {
        this.debuggingPort = port;
        console.log("Enabled debugging port:", port);
        return this;
    };

    this.setFrameIndex = function(idx) {
        this.frameIndex = idx;
    };

    this.setPageId = function(s) {
        if (s == null) {
            var pageList = this.getPageList();
            if (pageList instanceof Array && pageList.length > 0) {
                this.pageId = pageList[0].id;
            }
        } else {
            this.pageId = s;
        }
    };

    this.createShoutcut = function(url) {
        if (!this.userDataDir) {
            this.setUserDataDir(null);
        }

        var cmd = [
            this.binPath
        ];

        // disable default browser check
        cmd.push("--no-default-browser-check");

        // disable popop blocking
        cmd.push("--disable-popup-blocking");

        // disable 3d
        cmd.push("--disable-3d-apis");

        // block non-proxyed webrtc traffic
        cmd.push("--force-webrtc-ip-handling-policy=disable-non-proxied-udp");

        // check incognito mode
        if (this.inPrivate) {
            cmd.push("--incognito");
        }

        // check debugging port
        if (this.debuggingPort > 0) {
            cmd.push("--remote-debugging-port=" + this.debuggingPort);
        }

        cmd.push("\"--profile-directory=" + this.profileName + "\"");
        
        // set user data directory
        if (this.userDataDir != null) {
            cmd.push("\"--user-data-dir=" + this.userDataDir + "\"");
        }
        
        cmd.push("\"" + url + "\"");

        SHELL.createShoutcut(publisherName.get() + " (" + this.profileName + ")", cmd.join(' '), SYS.getCurrentScriptDirectory());
    };

    this.setInPrivate = function(flag) {
        this.inPrivate = flag;
        return this;
    };

    this.setUserAgent = function(userAgent) {
        this.userAgent = userAgent;
        return this;
    };

    this.addUserAgent = function(userAgent) {
        this.userAgents.push(userAgent);
        return this;
    };

    this.addUserAgentsFromFile = function(filename) {
        var text = FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8);
        var lines = text.split(/\r?\n/);
        for (var i = 0; i < lines.length; i++) {
            var line = lines[i].trim();
            if (line !== "") {
                this.userAgents.push(line);
            }
        }
        return this;
    };

    this.open = function(url) {
        this.setProfile(this.profileName, this.installedDir);
        
        // if the file does not exists, Check the 32bit installation folder again
        if (!FILE.fileExists(this.binPath)) {
            this.setPublisherName("chrome.x86");
            this.setProfile(this.profileName, this.installedDir);
        }

        // find a executable file
        if (!FILE.fileExists(this.binPath)) {
            console.error("chrome.exe does not exists.");
            return this;
        }
        
        try {
            // get user data directory
            if (!this.userDataDir) {
                this.setUserDataDir(null);
            }

            // connect to shell
            var shell = SHELL.create();
            shell.setWorkingDirectory(this.workingDirectory);

            // initialize
            var cmd = [];

            // enable inPrivate (incognito) mode
            if (this.inPrivate) {
                cmd.push("--incognito");
            }

            // enable debugging port
            if (this.debuggingPort > 0) {
                cmd.push("--remote-debugging-port=" + this.debuggingPort);
            }

            // disable default browser check
            cmd.push("--no-default-browser-check");

            // disable popop blocking
            cmd.push("--disable-popup-blocking");

            // disable 3D
            cmd.push("--disable-3d-apis");

            // block non-proxyed webrtc traffic
            cmd.push("--force-webrtc-ip-handling-policy=disable-non-proxied-udp");
            
            // disable session crashed bubble (
            //cmd.push("--disable-session-crashed-bubble");

            // enable restore the last session
            //cmd.push("--restore-last-session");

            // set profile directory
            cmd.push("\"--profile-directory=" + this.profileName + "\"");

            // set proxy configuration
            if (this.proxy != null && this.isPreventProxy != true) {
                console.log("Enabled proxy server:", this.proxy.protocol + "://" + this.proxy.host + ":" + this.proxy.port);
                cmd.push("--proxy-server=\"" + this.proxy.protocol + "://" + this.proxy.host + ":" + this.proxy.port + "\"");
                if (this.proxy.userAgent != null) {
                    this.setUserAgent(this.proxy.userAgent);
                }
            }

            // set user data directory
            if (this.userDataDir != null) {
                cmd.push("\"--user-data-dir=" + this.userDataDir + "\"");
            }

            // choice user agent
            if (this.userAgent == null && this.userAgents.length > 0) {
                this.setUserAgent(RAND.one(this.userAgents));
            }

            // set user agent
            if (this.userAgent != null) {
                cmd.push("\"--user-agent=" + this.userAgent + "\"");
            }
            
            // disable unwanted features
            if (this.disableFeatures.length > 0) {
                cmd.push("\"--disable-features=" + this.disableFeatures.join(',') + "\"");
            }

            // set the URL
            cmd.push((this.isAppMode ? "--app=\"" : "\"") + url + "\"");

            // build the command line
            console.log(cmd.join(" "));

            // run
            shell.runAs(this.binPath, cmd);
            sleep(300);

            // release shell
            shell.release();
        } catch (e) {
            console.error("Error opening a browser: " + e.message);
            sleep(300);
        }

        return this;
    };
    
    this.getPageList = function() {
        var pageList = [];

        if (this.debuggingPort > 0) {
            try {
                var responseText = HTTP.get("http://127.0.0.1:" + this.debuggingPort + "/json");
                pageList = JSON.parse(responseText);
                
                this.pageList = pageList;  // save the page list
            } catch (e) {
                console.error("ChromeObject.getPageList() ->", e.message);
            }
        } else {
            console.error("Remote debugging unavailable");
        }
        
        return pageList;
    };

    this.getPageById = function(id) {
        return this.getPageList().find(function(x) {
            return (x.id == id);
        });
    };

    this.getPagesByUrl = function(url) {
        return this.getPageList().filter(function(x) {
            return (x.url.indexOf(url) == 0);
        });
    };

    this.getPagesByUrls = function(urls) {
        return this.getPageList().reduce(function(acc, x) {
            for (var i = 0; i < urls.length; i++) {
                if (x.url.indexOf(urls[i]) == 0) {
                    acc.push(x);
                }
            }
            return acc;
        }, []);
    };

    this.getPagesByTitle = function(title) {
        return this.getPageList().filter(function(x) {
            return (x.title.indexOf(title) == 0);
        });
    };

    this.getPagesByTitles = function(titles) {
        return this.getPageList().reduce(function(acc, x) {
            for (var i = 0; i < titles.length; i++) {
                if (x.title.indexOf(titles[i]) == 0) {
                    acc.push(x);
                }
            }
            return acc;
        }, []);
    };

    this.getParameterValue = function(paramName, defaultValue) {
        var paramValue = defaultValue;
        var page = this.getPageById(this.pageId);
        var params = page.url.split('&');

        for (var i = 0; i < params.length; i++) {
            if (params[i].indexOf(paramName + '=') == 0) {
                paramValue = params[i].substring((paramName + '=').length);
                //break;  // Do not break because will be get the last value
            }
        }

        return paramValue;
    };

    this.sendPageRPC = function(method, params) {
        var response = null;

        try {
            if (this.pageId != "") {
                var url = "http://localhost:3000/devtools/page/" + this.pageId + "?port=" + this.debuggingPort;
                try {
                    response = HTTP.create()
                        .setContentType("application/json")
                        .setDataType("json")
                        .open("POST", url)
                        .setRequestBody({
                            "id": pageEventId,
                            "method": method,
                            "params": params
                        })
                        .send()
                        .responseBody
                    ;
                    
                    // response if error
                    if ("result" in response) {
                        (function(r) {
                            if ("subtype" in r && r.subtype == "error") {
                                console.warn("[WebBrowser]", r.description);
                            }
                        })(response.result.result);
                    }
                } catch (e) {
                    console.warn(e.message);
                    response = {};
                }
                
                pageEventId.set(pageEventId.get() + 1);
                console.log("Sent the RPC message");
            } else {
                this.setPageId(null);
                if (this.pageId != "") {
                    response = this.sendPageRPC(method, params);
                } else {
                    console.error("Page not found");
                }
            }
        } catch (e) {
            console.log("ChromeObject.sendPageRPC() ->", e.message);
        }

        return response;
    };

    this.navigate = function(url) {
        return this.sendPageRPC("Page.navigate", {
            "url": url
        });
    };
    
    this.navigateEvaluately = function(url) {
        return this.evaluate('location.href = "' + url + '"');
    };

    this.evaluate = function(expression, frameIndex) {
        if (this.isPreventEvaluate) return;

        var frameIndex = (typeof frameIndex !== "undefined" ? frameIndex : this.frameIndex);

        if (frameIndex > -1) {
            expression = 'var __getFrame=function(e){return document.getElementsByTagName("frame")[e]},__getDocument=function(){return __getFrame(' + frameIndex + ').contentDocument||__getFrame(' + frameIndex + ').contentWindow.document},__getWindow=function(){return __getFrame(' + frameIndex + ').contentWindow};' + expression;
        } else {
            expression = 'var __getDocument=function(){return document},__getWindow=function(){return window};' + expression;
        }

        try {
            return this.sendPageRPC("Runtime.evaluate", {
                "expression": expression
            });
        } catch (e) {
            console.error("ChromeObject.evaluate() ->", e.message);
        }
    };

    this.getEvaluatedValue = function(expression) {
        try {
            var response = this.evaluate(expression);
            return (function(r) {
                return ("value" in r ? r.value : "");
            })(response.result.result);
        } catch (e) {
            console.error("ChromeObject.getEvaluatedValue() ->", e.message);
            return "";
        }
    };

    this.exit = function() {
        return this.sendPageRPC("Browser.close", {});
    };
    
    this.close = function() {
        var response = this.sendPageRPC("Page.close", {});
        this.setPageId(null);
        return response;
    };

    this.terminate = function() {
        try {
            this.oAutoIt.callFunction("WinKill", [this.getTitle()]);
        } catch (e) {
            console.error("ChromeObject.terminate() ->", e.message);
        }
    };

    this.focus = function(isActivate) {
        var title = "";
        isActivate = (typeof isActivate === "undefined" ? false : isActivate);

        if (this.debuggingPort > 0) {
            try {
                // set page id
                if (this.pageId == "") {
                    this.setPageId(null);
                }

                // calling _focus()
                title = this._focus();

                // find window by title
                if (isActivate) {
                    var pageList = this.getPageList();
                    if (pageList.length > 0) {
                        this.oAutoIt.callFunction("WinActivate", [title]);
                    }
                } else {
                    console.info("To move the window to the front, the isActivate flag must be set to true.");
                }
            } catch (e) {
                console.error("ChromeObject.focus() ->", e.message);
            }
        }

        // calling `onfocus` event
        this.dispatchEvent(new STD.Event("focus"));

        return title;
    };

    this._focus = function() {
        var title = "";
        
        try {
            // get current title
            var _title = this.getTitle();

            // if not focused
            if (_title.indexOf(this.pageId.substring(0, 6)) < 0) {
                // save previous title
                this.title = _title;

                // will be change title
                title = this.title + " " + this.pageId.substring(0, 6);

                // change webpage title for focusing window
                this.setTitle(title);
            } else {
                title = _title;   /// when it is already catch
            }
        } catch (e) {
            console.error("ChromeObject._focus() ->", e.message);
        }

        return title;
    };

    this.blur = function() {
        return this.setTitle(this.title);
    };

    this.getScrollHeight = function(selector) {
        return parseInt(this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").scrollHeight'));
    };

    this.focusWithoutActivate = function() {
        if (this.debuggingPort > 0) {
            try {
                // if page ID is empty
                if (this.pageId == "") {
                    var pageList = this.getPageList();
                    if (pageList instanceof Array && pageList.length > 0) {
                        this.pageId = pageList[0].id;
                    }
                }

                // change webpage title for focusing window
                this.setTitle(this.pageId);
            } catch (e) {
                console.error("ChromeObject.focusWithoutActivate() ->", e.message);
            }
        }
    };

    this.autoAdjustByScreen = function(sX, sY, divX, divY) {
        // focus
        var title = this.focus();
        sleep(300);

        // adjust window position and size
        var bX = Math.floor(sX / divX);
        var bY = Math.floor(sY / divY);
        var x = this.getRandomInt(0, bX);
        var y = this.getRandomInt(0, bY);
        var w = this.getRandomInt(bX * 3, sX - bX);
        var h = this.getRandomInt(bY, sY - bY);
        this.oAutoIt.callFunction("WinMove", [title, "", x, y, w, h]);
        
        // blur
        this.blur();
    };

    this.autoAdjustByWindow = function(sX, sY, w1, w2, h1, h2) {
        // catch focus
        var title = this.focus();
        sleep(300);

        // adjust window position and size
        var w = this.getRandomInt(w1, w2);
        var h = this.getRandomInt(h1, h2);
        var x = this.getRandomInt(0, (sX - w < 0 ? parseInt(sX * 0.2) : (sX - w)));
        var y = this.getRandomInt(0, (sY - h < 0 ? parseInt(sY * 0.2) : (sY - h)));
        this.oAutoIt.callFunction("WinMove", [title, "", x, y, w, h]);

        // blur
        this.blur();
    };
    
    this.downMouseWheel = function(times) {
        if (this.debuggingPort > 0) {
            try {
                var pos = this.getScreenPosition();
                this.mouseMove(pos.x + 100, pos.y + 100);
                this.oAutoIt.callFunction("MouseWheel", ["down", times]);
            } catch (e) {
                console.error("ChromeObject.downMouseWheel() ->", e.message);
            }
        }
    };

    this.upMouseWheel = function(times) {
        if (this.debuggingPort > 0) {
            try {
                var pos = this.getScreenPosition();
                this.mouseMove(pos.x + 100, pos.y + 100);
                this.oAutoIt.callFunction("MouseWheel", ["up", times]);
            } catch (e) {
                console.error("ChromeObject.upMouseWheel() ->", e.message);
            }
        }
    };
    
    this.setTitle = function(title) {
        if (!(this.debuggingPort > 0))
            return;
        
        var i = 0, k = 60;
        while (i < k && title != this.getTitle()) {
            sleep(1000);
            this.evaluate('document.title = ' + this.__escape(title));
            i++;
        }
    };

    this.getTitle = function() {
        if (!(this.debuggingPort > 0))
            return;

        var i = 0, k = 60, title = "";
        while (i < k && title == "") {
            sleep(1000);
            title = this.getEvaluatedValue('document.title')
            i++;
        }
        
        return title;
    };

    this.getScreenPosition = function() {
        var result = this.getEvaluatedValue('(function() { return [__getWindow().screenX, __getWindow().screenY].join(","); })();');
        var pos = result.split(',');
        return {
            "x": parseInt(pos[0]),
            "y": parseInt(pos[1])
        };
    };

    this.getElementPosition = function(selector, startIndex) {
        var startIndex = (typeof startIndex !== 'undefined' ? startIndex : 0);
        var result;
        var pos = -1;

        if (startIndex > 0) {
            result = this.getEvaluatedValue('(function(k) { var rect = __getDocument().querySelectorAll("' + selector + '")[k].getBoundingClientRect(); return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height)].join(","); })(' + startIndex + ');');
        } else {
            result = this.getEvaluatedValue('(function() { var rect = __getDocument().querySelector("' + selector + '").getBoundingClientRect(); return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height)].join(","); })();');
        }

        pos = result.split(',');
        if (pos.length == 6) {
            return {
                "x": parseInt(pos[0]),
                "y": parseInt(pos[1]),
                "a": parseInt(pos[2]),
                "b": parseInt(pos[3]),
                "w": parseInt(pos[4]),
                "h": parseInt(pos[5])
            };
        } else {
            return {
                "x": -1,
                "y": -1,
                "a": -1,
                "b": -1,
                "g": -1,
                "d": -1
            };
        }
    };

    this.getDeepElementPosition = function(selectors) {
        var result = this.getEvaluatedValue('(function() { var rect = __getDocument()' + this.getShadowRootSelector(selectors) + '.getBoundingClientRect(); return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height)].join(","); })();');
        var pos = result.split(',');
        
        if (pos.length == 6) {
            return {
                "x": parseInt(pos[0]),
                "y": parseInt(pos[1]),
                "a": parseInt(pos[2]),
                "b": parseInt(pos[3]),
                "w": parseInt(pos[4]),
                "h": parseInt(pos[5])
            };
        } else {
            return {
                "x": -1,
                "y": -1,
                "a": -1,
                "b": -1,
                "g": -1,
                "d": -1
            };
        }
    };

    this.getElementPositionByText = function(selector, searchText) {
        var result;
        var pos = -1;
        var s = '(function() {'
            + '    var element = Object.values(__getDocument().querySelectorAll("' + selector + '")).find(function(x) {'
            + '        return (x.innerText.indexOf(' + this.__escape() + ') > -1);'
            + '    });'
            + '    if (element) {'
            + '        var rect = element.getBoundingClientRect();'
            + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height)].join(",");'
            + '    }'
            + '})()'
        ;

        result = this.getEvaluatedValue(s);
        pos = result.split(',');
        if (pos.length == 6) {
            return {
                "x": parseInt(pos[0]),
                "y": parseInt(pos[1]),
                "a": parseInt(pos[2]),
                "b": parseInt(pos[3]),
                "w": parseInt(pos[4]),
                "h": parseInt(pos[5])
            };
        } else {
            return {
                "x": -1,
                "y": -1,
                "a": -1,
                "b": -1,
                "g": -1,
                "d": -1
            };
        }
    };

    this.getNestedElementPosition = function(selector, subSelector, searchText, startIndex) {
        var s = '';
        var startIndex = (typeof startIndex !== 'undefined' ? startIndex : 0);

        if (searchText.indexOf(':tokenize(') == 0) {
            searchText = searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')'));
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = ' + (subSelector == ':self' ? 'x;' : 'x.querySelector("' + subSelector + '");')
                + '        var keywords = ' + this.__escape(searchText) + '.trim().split(" ");'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.split(" ").filter(function(w) { return keywords.indexOf(w) > -1; }).length >= keywords.length);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        var element = elements[0];'
                + '        var rect = element.getBoundingClientRect();'
                + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                + '    } else {'
                + '        return "";'
                + '    }'
                + '})()'
            ;
        }

        else if (searchText.indexOf(':text(') == 0) {
            this.evaluate(ExtraMath.export_measureSimilarity());

            searchText = searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')'));
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = ' + (subSelector == ':self' ? 'x;' : 'x.querySelector("' + subSelector + '");')
                + '        var searchText = ' + this.__escape(searchText) + '.trim();'
                + '        var text = el instanceof HTMLElement ? el.innerText : "";'
                + '        return ExtraMath.measureSimilarity(text, searchText) >= 0.9;'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        var element = elements[0];'
                + '        var rect = element.getBoundingClientRect();'
                + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                + '    } else {'
                + '        return "";'
                + '    }'
                + '})()'
            ;
        }

        else if (searchText.indexOf(':p(') == 0) {
            var p = parseFloat(searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')')));
            if (p > 0) {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                    + '        return (Math.random() < ' + p + ');'
                    + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                    + '    if (elements.length > 0) {'
                    + '        var element = elements[0];'
                    + '        var rect = element.getBoundingClientRect();'
                    + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                    + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                    + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                    + '    } else {'
                    + '        return "";'
                    + '    }'
                    + '})()'
                ;
            } else {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '"));'
                    + '    ' + (startIndex > 0 ? 'elements = elements.slice(' + startIndex + ');' : '')
                    + '    if (elements.length > 0) {'
                    + '        var k = Math.floor(Math.random() * elements.length);'
                    + '        var element = elements[k];'
                    + '        var rect = element.getBoundingClientRect();'
                    + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                    + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                    + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                    + '    } else {'
                    + '        return "";'
                    + '    }'
                    + '})()'
                ;
            }
        }

        else {
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = ' + (subSelector == ':self' ? 'x;' : 'x.querySelector("' + subSelector + '");')
                + '        var searchText = ' + this.__escape(searchText) + '.trim();'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.indexOf(searchText) > -1);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        var element = elements[0];'
                + '        var rect = element.getBoundingClientRect();'
                + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                + '    } else {'
                + '        return "";'
                + '    }'
                + '})()'
            ;
        }

        var result = this.getEvaluatedValue(s);
        var pos = result.split(',');
        if (pos.length == 7) {
            return {
                "x": parseInt(pos[0]),
                "y": parseInt(pos[1]),
                "a": parseInt(pos[2]),
                "b": parseInt(pos[3]),
                "w": parseInt(pos[4]),
                "h": parseInt(pos[5]),
                "s": pos[6]
            };
        } else {
            return {
                "x": -1,
                "y": -1,
                "a": -1,
                "b": -1,
                "w": -1,
                "h": -1,
                "s": ""
            };
        }
    };

    this.triggerEventOnNestedElement = function(eventName, selector, subSelector, searchText, startIndex) {
        var s = '';
        var startIndex = (typeof startIndex !== 'undefined' ? startIndex : 0);

        if (searchText.indexOf(':tokenize(') == 0) {
            searchText = searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')'));
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = ' + (subSelector == ':self' ? 'x;' : 'x.querySelector("' + subSelector + '");')
                + '        var keywords = ' + this.__escape(searchText) + '.trim().split(" ");'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.split(" ").filter(function(w) { return keywords.indexOf(w) > -1; }).length >= keywords.length);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        elements[0].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                + '    }'
                + '})()'
            ;
        } else if (searchText.indexOf(':p(') == 0) {
            var p = parseFloat(searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')')));
            if (p > 0) {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                    + '        return (Math.random() < ' + p + ');'
                    + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                    + '    if (elements.length > 0) {'
                    + '        elements[0].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                    + '    }'
                    + '})()'
                ;
            } else {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '"));'
                    + '    ' + (startIndex > 0 ? 'elements = elements.slice(' + startIndex + ');' : '')
                    + '    if (elements.length > 0) {'
                    + '        var k = Math.floor(Math.random() * elements.length);'
                    + '        elements[k].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                    + '    }'
                    + '})()'
                ;
            }
        } else {
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = ' + (subSelector == ':self' ? 'x;' : 'x.querySelector("' + subSelector + '");')
                + '        var searchText = ' + this.__escape(searchText) + '.trim();'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.indexOf(searchText) > -1);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        elements[0].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                + '    }'
                + '})()'
            ;
        }

        return this.evaluate(s);
    };

    this.getNestedElementIndex = function(selector, subSelector, searchText) {
        var s = '';

        s += '(function() {'
            + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '"));'
            + '    var result = -1;'
            + '    for (var i = 0; i < elements.length; i++) {'
            + '        if (x.querySelector("' + subSelector + '").innerText.indexOf(' + this.__escape(searchText) + ') > -1) {'
            + '            result = i;'
            + '            break;'
            + '        }'
            + '    }'
            + '    return result;'
            + '})()'
        ;

        return parseInt(this.getEvaluatedValue(s));
    };

    this.getElementCount = function(selector) {
        return this.getEvaluatedValue('document.querySelectorAll("' + selector + '").length');
    };

    this.getPageHeight = function() {
        var height = 0;

        if (this.debuggingPort > 0) {
            var result = this.getEvaluatedValue('(function(obj) { return Math.max(obj.scrollHeight, obj.clientHeight); })(__getDocument().querySelector("html"))');
            height = parseInt(result);
        }

        return height;
    };
    
    this.setIsAttached = function(isAttached) {
        this.isAttached = isAttached;
        return this;
    };

    this.getRandomInt = function(min, max) {
        min = Math.ceil(min);
        max = Math.floor(max);
        return Math.floor(Math.random() * (max - min + 1)) + min;
    };
    
    this.checkDebuggingPort = function() {
        var isChecked = false;
        var isDone = false;

        while (!isDone) {
            try {
                if (this.debuggingPort > 0) {
                    var result = SHELL.exec("netstat -ano | findstr :" + this.debuggingPort);
                    if (result.indexOf(":" + this.debuggingPort) > -1) {
                        isChecked = true;
                    }
                }

                isDone = true;
            } catch(e) {
                sleep(1);
                console.error("ChromeObject.checkDebuggingPort() ->", e.message);
            }
        }

        return isChecked;
    };

    this.getCurrentUrl = function() {
        var page = this.getPageById(this.pageId);
        return page.url;
    };

    this.getCurrentDomain = function() {
        return this.getEvaluatedValue('__getDocument().domain') || '';
    };

    this.triggerEvent = function(eventName, selector) {
        if (selector.indexOf(':p(') < 0) {
            if (eventName == 'click') {
                return this.evaluate('__getDocument().querySelector("' + selector + '").click()');
            } else {
                return this.evaluate('__getDocument().querySelector("' + selector + '").dispatchEvent(new Event("' + eventName + '"))');
            }
        } else {
            var p = parseFloat(selector.substring(selector.indexOf('(') + 1, selector.indexOf(')')));
            var _selector = selector.substring(0, selector.indexOf(':'));
            if (p > 0) {
                return this.evaluate('(function(obj, p) { var element = Object.values(obj).find(function() { return (Math.random() < p); }); if(element) element.click(); })(__getDocument().querySelectorAll("' + _selector + '"), ' + p + ')');
            } else {
                return this.evaluate('(function(obj) { var elements = Object.values(obj); var element = elements[Math.floor(Math.random() * elements.length)]; if(element) element.click(); })(__getDocument().querySelectorAll("' + _selector + '"))');
            }
        }
    };

    this.triggerEventByFind = function(eventName, selector, searchText) {
        var s = '(function() {'
            + '    var element = Object.values(__getDocument().querySelectorAll("' + selector + '")).find(function(x) {'
            + '        return (x.innerText.indexOf(' + this.__escape(searchText) + ') > -1);'
            + '    });'
            + '    if (element) {'
            + '        element.' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
            + '    }'
            + '})()'
        ;
        return this.evaluate(s);
    };

    this.triggerEventOnNestedFind = function(eventName, selector, subSelector, searchText) {
        var s = '(function() {'
            + '    var element = Object.values(__getDocument().querySelectorAll("' + selector + '")).find(function(x) {'
            + '        return (x.querySelector("' + subSelector + '").innerText.indexOf(' + this.__escape(searchText) + ') > -1);'
            + '    });'
            + '    if (element) {'
            + '        element.' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
            + '    }'
            + '})()'
        ;
        return this.evaluate(s);
    };

    this.scrollTo = function(x, y) {
        return this.evaluate('__getWindow().scrollTo(parseInt(' + x + '), parseInt(' + y + '))');
    };

    this.scrollBy = function(dx, dy) {
        return this.evaluate('__getWindow().scrollBy(parseInt(' + dx + '), parseInt(' + dy + '))');
    };

    this.scrollToElement = function(selector, dx, dy) {
        return this.evaluate('(function(rect, dx, dy) { __getWindow().scrollTo(rect.x + dx, rect.y + dy); })(__getDocument().querySelector("' + selector + '").getBoundingClientRect(), parseInt("' + dx + '"), parseInt("' + dy + '"))');
    };

    this.reload = function() {
        //return this.sendPageRPC("Page.reload", {});
        return this.evaluate("__getWindow().reload()");
    };

    this.hasClass = function(seletctor, className) {
        try {
            var result = this.getEvaluatedValue('__getDocument().querySelector("' + seletctor + '").getAttribute("class")');
            if (typeof(result) === "string") {
                return (result.split(' ').indexOf(className) > -1);
            } else {
                return false;
            }
        } catch (e) {
            console.error("ChromeObject.hasClass() ->", e.message);
        }
    };

    this.getAttribute = function(selector, attributeName) {
        return this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").getAttribute("' + attributeName + '")');
    };

    this.sendKeys = function(s) {
        this.oAutoIt.send(s);
    };

    this.sendSpaceKey = function() {
        this.oAutoIt.send("{SPACE}");
    };

    this.setValue = function(selector, value, repeat, searchIndex) {
        var s = value,
            i = 0,
            searchIndex = (typeof searchIndex !== "undefined" ? searchIndex : 0),
            repeat = (typeof repeat !== "undefined" ? repeat : 1)
        ;

        while (i < repeat) {
            if (searchIndex > 0) {
                this.evaluate('Object.values(__getDocument().querySelectorAll("' + selector + '"))[' + searchIndex + '].value = ' + this.__escape(s));
            } else {
                this.evaluate('__getDocument().querySelector("' + selector + '").value = ' + this.__escape(s));
            }
            i++;
        }
    };

    this.getText = function(selector) {
        return this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").innerText');
    };

    this.setHTML = function(selector, value, repeat, searchIndex) {
        var s = value,
            i = 0,
            searchIndex = (typeof searchIndex !== "undefined" ? searchIndex : 0),
            repeat = (typeof repeat !== "undefined" ? repeat : 1)
        ;

        while (i < repeat) {
            if (searchIndex > 0) {
                this.evaluate('Object.values(__getDocument().querySelectorAll("' + selector + '"))[' + searchIndex + '].value = ' + this.__escape(s));
            } else {
                this.evaluate('__getDocument().querySelector("' + selector + '").innerHTML = ' + this.__escape(s));
            }
            i++;
        }
    };

    this.getHTML = function(selector) {
        return this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").innerHTML');
    };

    this.traceMouseClick = function() {
        return this.evaluate('__getWindow().addEventListener("click",function(e){var t=e.clientX,n=e.clientY,l=__getDocument().createElement("div");l.style.position="absolute",l.style.width="20px",l.style.height="20px",l.style.backgroundColor="#00ff00",l.style.zIndex=99999,l.style.top=__getWindow().pageYOffset+n-10+"px",l.style.left=__getWindow().pageXOffset+t-10+"px",__getDocument().body.appendChild(l)});');
    };

    // Added in 2023-12-27
    this.markPosition = function(x, y) {
        return this.evaluate('(function(x, y){var t=x,n=y,l=__getDocument().createElement("div");l.style.position="absolute",l.style.width="20px",l.style.height="20px",l.style.backgroundColor="#ff0000",l.style.zIndex=99999,l.style.top=__getWindow().pageYOffset+n-10+"px",l.style.left=__getWindow().pageXOffset+t-10+"px",__getDocument().body.appendChild(l)})(' + parseInt(x) + ', ' + parseInt(y) + ');');
    };

    // Added in 2023-12-27
    this.getTextsBySelectorAll = function(selector) {
        return JSON.parse(this.getEvaluatedValue('JSON.stringify(Object.values(__getDocument().querySelectorAll("' + selector + '")).reduce(function(a, x) { a.push(x.innerText); return a; }, []))'));
    };

    this.getWindowInnerHeight = function() {
        return parseInt(this.getEvaluatedValue('__getWindow().innerHeight'));
    };

    this.getWindowPageYOffset = function() {
        return this.getEvaluatedValue('__getWindow().pageYOffset');
    };
    
    this.getDocumentBodyOffsetHeight = function() {
        return this.getEvaluatedValue('__getDocument().body.offsetHeight');
    };

    this.getDocumentScrollTop = function() {
        return parseInt(this.getEvaluatedValue('__getDocument().documentElement.scrollTop'));
    };

    // formula: y > 0 and y + h < ih
    this.isVisibleElementInViewport = function(elementPosition) {
        return (elementPosition.y > 0 && (elementPosition.y + elementPosition.h < this.getWindowInnerHeight()));
    };

    this.isPageScrollEnded = function() {
        return (this.getWindowInnerHeight() + this.getWindowPageYOffset()) >= this.getDocumentBodyOffsetHeight;
    };

    this.__escape = function(value) {
        var pos = value.indexOf("__escaped:");
        if (pos === 0)
            return 'decodeURIComponent("' + value.substring(10) + '")';
        else
            return 'decodeURIComponent("' + this.encodeURIComponent(value)[0] + '")';
    };

    // Added in 2024-01-08
    this.encodeURI = function(s) {
        return [encodeURI(s), s];
    };
    
    // Added in 2024-01-08
    this.decodeURI = function(s) {
        if (s instanceof Array && s.length == 2) {
            return s[1];
        } else {
            return decodeURI(s);
        }
    };

    // Added in 2024-01-08
    this.encodeURIComponent = function(s) {
        return [encodeURIComponent(s), s];
    };

    // Added in 2024-01-08
    this.decodeURIComponent = function(s) {
        if (s instanceof Array && s.length == 2) {
            return s[1];
        } else {
            return decodeURIComponent(s);
        }
    };

    this.prompt = function(s) {
        return this.getEvaluatedValue('prompt("' + s + '")');
    };
    
    this.confirm = function(s) {
        return this.getEvaluatedValue('(confirm("' + s + '") ? "true" : "false")');
    };

    /**
     * @deprecated Use Chrome.setPublisherName instead of Chrome.setVendor
     */
    this.setVendor = function(vendor) {
        this.setPublisherName(vendor);
        console.warn("Deprecated: Please use setPublisherName");
        
        return this;
    };

    this.setPublisherName = function(_publisherName) {
        publisherName.set(_publisherName.toLowerCase());

        switch (publisherName.get()) {
            case "msedge":
                this.workingDirectory = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Microsoft\\Edge\\Application";
                this.binPath = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Microsoft\\Edge\\Application\\msedge.exe";
                this.defaultUserDataDir = SYS.getEnvString("LOCALAPPDATA") + "\\Microsoft\\Edge\\User Data";
                break;

            case "chrome":
                this.workingDirectory = SYS.getEnvString("PROGRAMFILES") + "\\Google\\Chrome\\Application";
                this.binPath = SYS.getEnvString("PROGRAMFILES") + "\\Google\\:installedDir\\Application\\chrome.exe";
                this.defaultUserDataDir = SYS.getEnvString("LOCALAPPDATA") + "\\Google\\Chrome\\User Data";
                break;
                
            case "chrome.x86":
                this.workingDirectory = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\Chrome\\Application";
                this.binPath = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\:installedDir\\Application\\chrome.exe";
                this.defaultUserDataDir = SYS.getEnvString("LOCALAPPDATA") + "\\Google\\Chrome\\User Data";
                break;

            case "chromium":
                this.workingDirectory = SYS.getEnvString("LOCALAPPDATA") + "\\Chromium\\Application";
                this.binPath = SYS.getEnvString("LOCALAPPDATA") + "\\Chromium\\Application\\chrome.exe";
                this.defaultUserDataDir = null; // Edit here
                break;

            case "opera":
                this.workingDirectory = SYS.getEnvString("LOCALAPPDATA") + "\\Programs\\Opera";
                this.binPath = SYS.getEnvString("LOCALAPPDATA") + "\\Programs\\Opera\\opera.exe";
                this.defaultUserDataDir = SYS.getEnvString("LOCALAPPDATA") + "\\Opera Software\\Opera Stable\\Default";
                break;
                
            case "whale":
                this.workingDirectory = SYS.getEnvString("PROGRAMFILES") + "\\Naver\\Naver Whale\\Application";
                this.binPath = SYS.getEnvString("PROGRAMFILES") + "\\Naver\\Naver Whale\\Application\\whale.exe";
                this.defaultUserDataDir = SYS.getEnvString("LOCALAPPDATA") + "\\Naver\\Naver Whale\\User Data";
                this.baseScreenY = 82;
                break;
            
            case "brave":
                this.workingDirectory = SYS.getEnvString("PROGRAMFILES") + "\\BraveSoftware\\Brave-Browser\\Application";
                this.binPath = SYS.getEnvString("PROGRAMFILES") + "\\BraveSoftware\\Brave-Browser\\Application\\brave.exe";
                this.defaultUserDataDir = SYS.getEnvString("LOCALAPPDATA") + "\\BraveSoftware\\Brave-Browser\\User Data";
                break;
            
            case "vivaldi":
                this.workingDirectory = SYS.getEnvString("LOCALAPPDATA") + "\\Vivaldi\\Application";
                this.binPath = SYS.getEnvString("LOCALAPPDATA") + "\\Vivaldi\\Application\\vivaldi.exe";
                this.defaultUserDataDir = SYS.getEnvString("LOCALAPPDATA") + "\\Vivaldi\\User Data";
                break;
        }
        
        if (!this.userDataDir) {
            this.setUserDataDir(null);
        }

        return this;
    };

    this.vMouseClick = function(x, y) {
        Toolkit.sendClick(this.pageId.substring(0, 6), this.baseScreenX + x, this.baseScreenY + y, 1);
    };

    this.vSendKeys = function(s) {
        Toolkit.sendKeys(this.pageId.substring(0, 6), s);
    };

    this.mouseClick = function(x, y) {
        var screenPosition = this.getScreenPosition();
        this.oAutoIt.mouseMove(screenPosition.x + this.baseScreenX + x, screenPosition.y + this.baseScreenY + y);
        this.oAutoIt.mouseClick("left");
    };

    this.mouseWheelUp = function() {
        this.oAutoIt.callFunction("MouseWheel", ["up"]);
    };

    this.mouseWheelDown = function() {
        this.oAutoIt.callFunction("MouseWheel", ["down"]);
    };

    this.getReadyState = function() {
        return this.getEvaluatedValue("document.readyState");
    };
    
    this.getCookie = function() {
        return this.getEvaluatedValue("document.cookie");
    };

    this.getNumberOfSelectorAll = function(selector) {
        return parseInt(this.getEvaluatedValue('document.querySelectorAll("' + selector + '").length'));
    };

    this.setValueOfSelectorAll = function(selector, s) {
        this.evaluate('document.querySelectorAll("' + selector + '").forEach(function(x){x.value = "' + s + '";})');
    };

    this.sendEnterKey = function() {
        this.evaluate('var ev=new KeyboardEvent("keydown",{bubbles:!0,cancelable:!0,keyCode:13});document.body.dispatchEvent(ev);');
    };

    this.getShadowRootSelector = function(selectors) {
        var s = "').shadowRoot.querySelector('";
        return ".querySelector('" + selectors.join(s) + "')";
    };

    this.requestFullscreen = function() {
        this.evaluate('document.documentElement.requestFullscreen();');
    };
    
    this.getDisableFeatureIndex = function(feature) {
        return this.disableFeatures.indexOf(feature);
    };

    this.isFeatureDisabled = function(feature) {
        return this.getDisableFeatureIndex(feature) > -1;
    };
    
    this.addDisableFeature = function(feature) {
        if (!this.isFeatureDisabled(feature)) {
            this.disableFeatures.push(feature);
        }
        
        return this;
    };

    this.removeDisableFeature = function(feature) {
        var index = this.getDisableFeatureIndex(feature);
        if (index > -1) {
            this.disableFeatures.splice(index, 1);
        }
        
        return this;
    };

    this.create();
};
ChromeObject.prototype = new STD.EventTarget();
ChromeObject.prototype.constructor = ChromeObject;

exports.create = function(profileName) {
    return (new ChromeObject()).setProfile(profileName, null);
};

exports.start = function(url, proxyPort, profileName, userDataDir, installedDir) {
    return (new ChromeObject())
        .setProxyPort(proxyPort)
        .setProfile(profileName, installedDir)
        .setUserDataDir(userDataDir)
        .open(url)
    ;
};

exports.startWithDebugging = function(url, proxy, profileName, debuggingPort) {
    return (new ChromeObject())
        .setProxy(proxy)
        .setProfile(profileName, null)
        .setUserDataDir(null)
        .setDebuggingPort(debuggingPort)
        .open(url)
    ;
};

exports.startWithDebuggingUA = function(url, proxy, profileName, debuggingPort) {
    return (new ChromeObject())
        .setProxy(proxy)
        .setProfile(profileName, null)
        .setUserDataDir(null)
        .setDebuggingPort(debuggingPort)
        .addUserAgentsFromFile("data\\UserAgents.txt")
        .open(url)
    ;
};

exports.startDebug = function(url, proxy, profileName, debuggingPort, isPreventProxy) {
    return (new ChromeObject())
        .setProxy(proxy)
        .setProfile(profileName, null)
        .setUserDataDir(null)
        .setDebuggingPort(debuggingPort)
        .setIsPreventProxy(isPreventProxy)
        //.addUserAgentsFromFile("data\\Chrome.txt")
        //.addUserAgentsFromFile("data\\Edge.txt")
        //.addUserAgentsFromFile("data\\Safari.txt")
        .open(url)
    ;
};

exports.startDebugInPrivate = function(url, proxy, profileName, debuggingPort, isPreventProxy) {
    return (new ChromeObject())
        .setProxy(proxy)
        .setProfile(profileName, null)
        .setUserDataDir(null)
        .setDebuggingPort(debuggingPort)
        .setIsPreventProxy(isPreventProxy)
        .setInPrivate(true)
        //.addUserAgentsFromFile("data\\Chrome.txt")
        //.addUserAgentsFromFile("data\\Edge.txt")
        //.addUserAgentsFromFile("data\\Safari.txt")
        .open(url)
    ;
};

exports.publisherName = publisherName;

exports.VERSIONINFO = "Chrome Web Browser Debugging Interface (chrome.js) version 0.5.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
